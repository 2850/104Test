using Xunit;
using SecuritiesTradingApi.Infrastructure;

namespace SecuritiesTradingApi.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldGenerateValidFormat()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.Contains(":", hash);
        var parts = hash.Split(':');
        Assert.Equal(2, parts.Length);
        
        // Check that both parts are valid Base64
        Assert.NotNull(Convert.FromBase64String(parts[0])); // Salt
        Assert.NotNull(Convert.FromBase64String(parts[1])); // Hash
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = PasswordHasher.HashPassword(password);

        // Act
        var result = PasswordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456@";
        var hash = PasswordHasher.HashPassword(password);

        // Act
        var result = PasswordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = PasswordHasher.HashPassword(password);
        var hash2 = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        
        // But both should verify correctly
        Assert.True(PasswordHasher.VerifyPassword(password, hash1));
        Assert.True(PasswordHasher.VerifyPassword(password, hash2));
    }

    [Fact]
    public void HashPassword_WithNullPassword_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PasswordHasher.HashPassword(null!));
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ShouldThrowArgumentNullException()
    {
        // Arrange
        var hash = PasswordHasher.HashPassword("Test123!");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PasswordHasher.VerifyPassword(null!, hash));
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PasswordHasher.VerifyPassword("Test123!", null!));
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ShouldThrowFormatException()
    {
        // Arrange
        var invalidHash = "InvalidHashWithoutColon";

        // Act & Assert
        Assert.Throws<FormatException>(() => PasswordHasher.VerifyPassword("Test123!", invalidHash));
    }

    [Fact]
    public void HashPassword_WithEmptyString_ShouldGenerateHash()
    {
        // Arrange
        var password = "";

        // Act
        var hash = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.True(PasswordHasher.VerifyPassword(password, hash));
    }
}
