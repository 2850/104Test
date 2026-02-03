using System;
using System.Security.Cryptography;
using System.Text;

class PasswordHashGenerator
{
    static void Main(string[] args)
    {
        Console.WriteLine("Generating password hashes...");
        Console.WriteLine();
        Console.WriteLine("admin (Admin@123):");
        Console.WriteLine(HashPassword("Admin@123"));
        Console.WriteLine();
        Console.WriteLine("user1 (User1@123):");
        Console.WriteLine(HashPassword("User1@123"));
        Console.WriteLine();
        Console.WriteLine("user2 (User2@123):");
        Console.WriteLine(HashPassword("User2@123"));
    }
    
    static string HashPassword(string password)
    {
        const int SaltSize = 32;
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);
        
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(saltedPassword);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    }
}
