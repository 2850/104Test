using Microsoft.EntityFrameworkCore;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Infrastructure;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Services;

/// <summary>
/// 認證服務實作
/// </summary>
public class AuthService : IAuthService
{
    private readonly TradingDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _refreshTokenExpirationDays;

    public AuthService(
        TradingDbContext context,
        IJwtService jwtService,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _configuration = configuration;
        _refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
    }

    /// <inheritdoc/>
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // 查詢使用者
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found. Username={Username}", request.Username);
                return null;
            }

            // 驗證密碼
            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password. Username={Username}, UserId={UserId}", 
                    request.Username, user.UserId);
                return null;
            }

            // 產生 tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // 儲存 refresh token
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login successful. Username={Username}, UserId={UserId}", 
                user.Username, user.UserId);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtService.GetAccessTokenExpirationMinutes() * 60,
                Username = user.Username,
                Role = user.Role == UserRole.Admin ? "Admin" : "User"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login. Username={Username}", request.Username);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // 查詢 refresh token
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Token refresh failed: Token not found");
                return null;
            }

            // 檢查是否已撤銷
            if (storedToken.IsRevoked)
            {
                _logger.LogWarning("Token refresh failed: Token is revoked. UserId={UserId}", storedToken.UserId);
                return null;
            }

            // 檢查是否過期
            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Token refresh failed: Token expired. UserId={UserId}, ExpiresAt={ExpiresAt}", 
                    storedToken.UserId, storedToken.ExpiresAt);
                return null;
            }

            // 產生新的 tokens
            var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // 撤銷舊的 refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // 儲存新的 refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = storedToken.UserId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refresh successful. UserId={UserId}, Username={Username}", 
                storedToken.UserId, storedToken.User.Username);

            return new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtService.GetAccessTokenExpirationMinutes() * 60,
                Username = storedToken.User.Username,
                Role = storedToken.User.Role == UserRole.Admin ? "Admin" : "User"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeTokenAsync(string refreshToken, int userId)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (storedToken == null)
            {
                _logger.LogWarning("Token revocation failed: Token not found. UserId={UserId}", userId);
                return false;
            }

            if (storedToken.IsRevoked)
            {
                _logger.LogInformation("Token already revoked. UserId={UserId}", userId);
                return true;
            }

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token revoked successfully. UserId={UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation. UserId={UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow || rt.IsRevoked)
                .ToListAsync();

            var count = expiredTokens.Count;

            if (count > 0)
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired/revoked refresh tokens", count);
            }
            else
            {
                _logger.LogInformation("No expired/revoked refresh tokens to clean up");
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token cleanup");
            throw;
        }
    }
}
