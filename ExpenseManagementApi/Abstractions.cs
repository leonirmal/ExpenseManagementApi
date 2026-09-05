namespace ExpenseManagement.Core;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    void Remove(Category category);
}

public interface IExpenseRepository
{
    IQueryable<Expense> Query();
    Task<Expense?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task AddAsync(Expense expense, CancellationToken ct);
    void Remove(Expense expense);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IBudgetRepository
{
    IQueryable<Budget> Query();
    Task<Budget?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task AddAsync(Budget budget, CancellationToken ct);
    void Remove(Budget budget);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}

public interface IExpenseService
{
    Task<PagedResult<ExpenseResponse>> GetAsync(Guid userId, ExpenseQuery query, CancellationToken ct);
    Task<ExpenseResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task<ExpenseResponse> CreateAsync(Guid userId, ExpenseRequest request, CancellationToken ct);
    Task<ExpenseResponse?> UpdateAsync(Guid id, Guid userId, ExpenseRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct);
    Task<SpendingSummary> GetSummaryAsync(Guid userId, int year, int month, CancellationToken ct);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct);
    Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken ct);
    Task<CategoryResponse?> UpdateAsync(Guid id, CategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetResponse>> GetAsync(Guid userId, int? year, int? month, CancellationToken ct);
    Task<BudgetResponse> CreateAsync(Guid userId, BudgetRequest request, CancellationToken ct);
    Task<BudgetResponse?> UpdateAsync(Guid id, Guid userId, BudgetRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct);
}

public sealed record ExpenseQuery(
    int Page = 1,
    int PageSize = 10,
    Guid? CategoryId = null,
    DateTime? From = null,
    DateTime? To = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? Search = null,
    string SortBy = "date",
    string SortDirection = "desc");
