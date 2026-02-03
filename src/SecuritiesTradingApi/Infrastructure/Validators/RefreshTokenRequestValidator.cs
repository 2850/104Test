using FluentValidation;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Infrastructure.Validators;

/// <summary>
/// Refresh Token 請求驗證器
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh Token 為必填");
    }
}
