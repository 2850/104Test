using System.Security.Cryptography;
using System.Text;

namespace SecuritiesTradingApi.Infrastructure;

/// <summary>
/// 密碼雜湊工具類別（使用 SHA256 + salt）
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 32; // 32 bytes = 256 bits

    /// <summary>
    /// 產生密碼雜湊（格式：Base64Salt:Base64Hash）
    /// </summary>
    /// <param name="password">明文密碼</param>
    /// <returns>雜湊後的密碼字串</returns>
    /// <exception cref="ArgumentNullException">當密碼為 null 時拋出</exception>
    public static string HashPassword(string password)
    {
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        // 產生隨機 salt
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 計算 hash
        byte[] hash = ComputeHash(password, salt);

        // 返回格式：Base64Salt:Base64Hash
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// 驗證密碼是否正確
    /// </summary>
    /// <param name="password">明文密碼</param>
    /// <param name="storedHash">儲存的雜湊字串（格式：Base64Salt:Base64Hash）</param>
    /// <returns>密碼是否正確</returns>
    /// <exception cref="ArgumentNullException">當參數為 null 時拋出</exception>
    /// <exception cref="FormatException">當 storedHash 格式不正確時拋出</exception>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        if (storedHash == null)
        {
            throw new ArgumentNullException(nameof(storedHash));
        }

        // 解析 salt 和 hash
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid stored hash format. Expected format: Base64Salt:Base64Hash");
        }

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

        // 計算新的 hash
        byte[] computedHash = ComputeHash(password, salt);

        // 使用固定時間比較避免 timing attack
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }

    /// <summary>
    /// 計算密碼的 SHA256 雜湊
    /// </summary>
    private static byte[] ComputeHash(string password, byte[] salt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        
        // 合併 salt 和 password
        byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);

        // 使用 SHA256 計算 hash
        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(saltedPassword);
        }
    }
}
