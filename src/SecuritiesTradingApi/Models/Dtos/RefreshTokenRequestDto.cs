using System.ComponentModel.DataAnnotations;

namespace SecuritiesTradingApi.Models.Dtos;

/// <summary>
/// Refresh Token 請求 DTO
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh Token
    /// </summary>
    [Required(ErrorMessage = "Refresh Token 為必填")]
    public string RefreshToken { get; set; } = string.Empty;
}
