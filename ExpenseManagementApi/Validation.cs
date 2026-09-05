using ExpenseManagement.Core;
using FluentValidation;

namespace ExpenseManagement.Api;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");
    }
}

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress(); RuleFor(x => x.Password).NotEmpty(); }
}

public sealed class CategoryValidator : AbstractValidator<CategoryRequest>
{
    public CategoryValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(80); RuleFor(x => x.Description).MaximumLength(300); }
}

public sealed class ExpenseValidator : AbstractValidator<ExpenseRequest>
{
    public ExpenseValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ExpenseDate).LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));
        RuleFor(x => x.PaymentMethod).MaximumLength(50);
    }
}

public sealed class BudgetValidator : AbstractValidator<BudgetRequest>
{
    public BudgetValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(10_000_000);
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
