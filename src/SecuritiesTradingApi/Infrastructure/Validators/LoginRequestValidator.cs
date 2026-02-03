using FluentValidation;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Infrastructure.Validators;

/// <summary>
/// 登入請求驗證器
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("使用者名稱為必填")
            .Length(3, 50).WithMessage("使用者名稱長度必須在 3 到 50 個字元之間");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼為必填")
            .MinimumLength(8).WithMessage("密碼長度必須至少 8 個字元")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("密碼必須包含至少一個大寫字母、一個小寫字母、一個數字和一個特殊字元 (@$!%*?&)");
    }
}
