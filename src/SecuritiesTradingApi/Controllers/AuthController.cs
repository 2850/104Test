using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecuritiesTradingApi.Models;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.Controllers;

/// <summary>
/// 認證控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入回應（包含 Access Token 和 Refresh Token）</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(new ErrorResponseDto(
                    ErrorCodes.INVALID_PASSWORD,
                    "使用者名稱或密碼錯誤"));
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login endpoint error");
            return StatusCode(500, new ErrorResponseDto("INTERNAL_ERROR", "登入時發生錯誤"));
        }
    }

    /// <summary>
    /// 更新 Access Token（使用 Refresh Token）
    /// </summary>
    /// <param name="request">Refresh Token 請求</param>
    /// <returns>新的 Access Token 和 Refresh Token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (response == null)
            {
                return Unauthorized(new ErrorResponseDto(
                    ErrorCodes.TOKEN_INVALID,
                    "Refresh Token 無效、已過期或已被撤銷"));
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token endpoint error");
            return StatusCode(500, new ErrorResponseDto("INTERNAL_ERROR", "更新 Token 時發生錯誤"));
        }
    }

    /// <summary>
    /// 登出（撤銷 Refresh Token）
    /// </summary>
    /// <param name="request">Refresh Token 請求</param>
    /// <returns>成功或失敗訊息</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ErrorResponseDto(
                    ErrorCodes.TOKEN_INVALID,
                    "無效的使用者 Token"));
            }

            var success = await _authService.RevokeTokenAsync(request.RefreshToken, userId);

            if (!success)
            {
                return Unauthorized(new ErrorResponseDto(
                    ErrorCodes.TOKEN_INVALID,
                    "Refresh Token 無效或不屬於目前使用者"));
            }

            return Ok(new { message = "登出成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout endpoint error");
            return StatusCode(500, new ErrorResponseDto("INTERNAL_ERROR", "登出時發生錯誤"));
        }
    }

    /// <summary>
    /// 清理過期的 Refresh Tokens（僅管理員可使用）
    /// </summary>
    /// <returns>清理的 token 數量</returns>
    [HttpDelete("cleanup-expired-tokens")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CleanupExpiredTokens()
    {
        try
        {
            var count = await _authService.CleanupExpiredTokensAsync();
            return Ok(new { message = $"已清理 {count} 個過期或已撤銷的 Refresh Token", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup expired tokens endpoint error");
            return StatusCode(500, new ErrorResponseDto("INTERNAL_ERROR", "清理 Token 時發生錯誤"));
        }
    }
}
