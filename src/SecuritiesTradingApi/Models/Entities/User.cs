using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecuritiesTradingApi.Models.Entities;

/// <summary>
/// 使用者實體
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>
    /// 使用者 ID
    /// </summary>
    [Key]
    public int UserId { get; set; }

    /// <summary>
    /// 使用者名稱（唯一）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密碼雜湊（格式：salt:hash）
    /// </summary>
    [Required]
    [StringLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 使用者角色
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 導航屬性：使用者的 Refresh Tokens
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
