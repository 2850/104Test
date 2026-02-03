using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Services;

/// <summary>
/// 認證服務介面
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入回應（包含 tokens）</returns>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// 更新 Access Token（使用 Refresh Token）
    /// </summary>
    /// <param name="refreshToken">Refresh Token</param>
    /// <returns>新的登入回應（包含新的 tokens）</returns>
    Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 撤銷 Refresh Token（登出）
    /// </summary>
    /// <param name="refreshToken">要撤銷的 Refresh Token</param>
    /// <param name="userId">使用者 ID</param>
    /// <returns>是否成功撤銷</returns>
    Task<bool> RevokeTokenAsync(string refreshToken, int userId);

    /// <summary>
    /// 清理已過期的 Refresh Tokens
    /// </summary>
    /// <returns>清理的 token 數量</returns>
    Task<int> CleanupExpiredTokensAsync();
}
