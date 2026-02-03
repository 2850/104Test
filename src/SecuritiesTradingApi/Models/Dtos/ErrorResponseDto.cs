namespace SecuritiesTradingApi.Models.Dtos;

/// <summary>
/// 錯誤回應 DTO
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// 錯誤代碼
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 建構子
    /// </summary>
    public ErrorResponseDto()
    {
    }

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="errorCode">錯誤代碼</param>
    /// <param name="message">錯誤訊息</param>
    public ErrorResponseDto(string errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message;
    }
}
