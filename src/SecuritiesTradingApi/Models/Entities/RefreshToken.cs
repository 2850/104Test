using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecuritiesTradingApi.Models.Entities;

/// <summary>
/// Refresh Token 實體
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    /// <summary>
    /// Token ID
    /// </summary>
    [Key]
    public long TokenId { get; set; }

    /// <summary>
    /// 使用者 ID（外鍵）
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Token 字串（唯一）
    /// </summary>
    [Required]
    [StringLength(128)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 過期時間
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 是否已撤銷
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤銷時間
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 導航屬性：所屬使用者
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
