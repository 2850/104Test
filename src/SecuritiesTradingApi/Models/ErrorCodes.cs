namespace SecuritiesTradingApi.Models;

/// <summary>
/// 錯誤代碼常數
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// 使用者不存在
    /// </summary>
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";

    /// <summary>
    /// 密碼錯誤
    /// </summary>
    public const string INVALID_PASSWORD = "INVALID_PASSWORD";

    /// <summary>
    /// Access Token 過期
    /// </summary>
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

    /// <summary>
    /// Refresh Token 過期
    /// </summary>
    public const string REFRESH_TOKEN_EXPIRED = "REFRESH_TOKEN_EXPIRED";

    /// <summary>
    /// Token 無效
    /// </summary>
    public const string TOKEN_INVALID = "TOKEN_INVALID";

    /// <summary>
    /// Token 已被撤銷
    /// </summary>
    public const string TOKEN_REVOKED = "TOKEN_REVOKED";

    /// <summary>
    /// 禁止存取（權限不足）
    /// </summary>
    public const string FORBIDDEN = "FORBIDDEN";

    /// <summary>
    /// 驗證失敗
    /// </summary>
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
}
