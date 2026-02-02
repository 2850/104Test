using FluentValidation;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Infrastructure.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0");

        RuleFor(x => x.StockCode)
            .NotEmpty()
            .WithMessage("StockCode is required")
            .MaximumLength(10)
            .WithMessage("StockCode cannot exceed 10 characters");

        RuleFor(x => x.OrderType)
            .Must(x => x == 1 || x == 2)
            .WithMessage("OrderType must be 1 (Buy) or 2 (Sell)");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(9999999.99m)
            .WithMessage("Price cannot exceed 9999999.99");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .Must(x => x % 1000 == 0)
            .WithMessage("Quantity must be in multiples of 1000 shares");
    }
}
