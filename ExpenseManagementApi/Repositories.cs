using ExpenseManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManagement.Infrastructure;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.FindAsync([id], ct).AsTask();

    public async Task AddAsync(User user, CancellationToken ct) => await db.Users.AddAsync(user, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Category category, CancellationToken ct) => await db.Categories.AddAsync(category, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public void Remove(Category category) => db.Categories.Remove(category);
}

public sealed class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public IQueryable<Expense> Query() => db.Expenses.AsNoTracking().Include(x => x.Category);
    public Task<Expense?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.Expenses.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public async Task AddAsync(Expense expense, CancellationToken ct) => await db.Expenses.AddAsync(expense, ct);
    public void Remove(Expense expense) => db.Expenses.Remove(expense);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class BudgetRepository(AppDbContext db) : IBudgetRepository
{
    public IQueryable<Budget> Query() => db.Budgets.AsNoTracking().Include(x => x.Category);
    public Task<Budget?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.Budgets.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public async Task AddAsync(Budget budget, CancellationToken ct) => await db.Budgets.AddAsync(budget, ct);
    public void Remove(Budget budget) => db.Budgets.Remove(budget);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
