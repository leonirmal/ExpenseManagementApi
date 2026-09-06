namespace ExpenseManagement.Core;

public enum UserRole { User, Admin }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public sealed class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public sealed class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string Role, Guid UserId, string FullName);

public record CategoryRequest(string Name, string? Description);
public record CategoryResponse(Guid Id, string Name, string? Description, bool IsActive);

public record ExpenseRequest(
    Guid CategoryId,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    string? PaymentMethod);

public record ExpenseResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    string? PaymentMethod,
    DateTime CreatedAtUtc);

public record BudgetRequest(Guid CategoryId, decimal Amount, int Year, int Month);
public record BudgetResponse(Guid Id, Guid CategoryId, string CategoryName, decimal Amount, int Year, int Month, decimal Spent, decimal Remaining, decimal UsagePercentage);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record SpendingSummary(decimal TotalSpent, decimal BudgetAmount, decimal RemainingBudget, decimal UsagePercentage);
