using Xunit;
using FluentValidation.TestHelper;
using SecuritiesTradingApi.Infrastructure.Validators;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.UnitTests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyUsername_ShouldHaveError(string username)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = username!,
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("ab")]  // Too short
    [InlineData("a")]
    public void Validate_WithTooShortUsername_ShouldHaveError(string username)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = username,
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithTooLongUsername_ShouldHaveError()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = new string('a', 51),  // 51 characters
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldHaveError(string password)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = password!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithTooShortPassword_ShouldHaveError()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Test@1"  // Only 6 characters
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("test1234")]  // No uppercase
    [InlineData("TEST1234")]  // No lowercase
    [InlineData("Testtest")]  // No digit
    [InlineData("Test1234")]  // No special character
    [InlineData("testtest")]  // No uppercase, no digit, no special
    public void Validate_WithPasswordMissingRequirements_ShouldHaveError(string password)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("Test@123")]
    [InlineData("Valid$Pass1")]
    [InlineData("Secure!234")]
    [InlineData("MyP@ssw0rd")]
    [InlineData("Complex&56")]
    public void Validate_WithValidPassword_ShouldNotHaveError(string password)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
