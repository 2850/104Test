using System.Security.Claims;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Services;

/// <summary>
/// JWT 服務介面
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 產生 Access Token
    /// </summary>
    /// <param name="user">使用者實體</param>
    /// <returns>JWT Token 字串</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// 產生 Refresh Token
    /// </summary>
    /// <returns>Refresh Token 字串</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// 從 Token 取得 ClaimsPrincipal
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <param name="validateLifetime">是否驗證過期時間</param>
    /// <returns>ClaimsPrincipal</returns>
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true);

    /// <summary>
    /// 取得 Access Token 過期時間（分鐘）
    /// </summary>
    /// <returns>過期時間（分鐘）</returns>
    int GetAccessTokenExpirationMinutes();
}
