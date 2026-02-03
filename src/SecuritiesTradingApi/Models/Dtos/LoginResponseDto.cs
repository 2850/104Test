namespace SecuritiesTradingApi.Models.Dtos;

/// <summary>
/// 登入回應 DTO
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Access Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token 類型（固定為 "Bearer"）
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Access Token 過期時間（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 使用者名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 使用者角色
    /// </summary>
    public string Role { get; set; } = string.Empty;
}
