using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseManagement.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseManagement.Infrastructure;

public sealed class AuthService(IUserRepository users, IConfiguration config) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, ct) is not null) return null;

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User
        };

        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return CreateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;
        return CreateToken(user);
    }

    private AuthResponse CreateToken(User user)
    {
        var jwt = config.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = jwt["Issuer"] ?? "ExpenseManagementApi";
        var audience = jwt["Audience"] ?? "ExpenseManagementClients";
        var minutes = int.TryParse(jwt["ExpiresMinutes"], out var value) ? value : 60;

        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Role.ToString(), user.Id, user.FullName);
    }
}

public sealed class CategoryService(ICategoryRepository repo) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct) =>
        (await repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await repo.GetByIdAsync(id, ct)) is { } c ? Map(c) : null;

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken ct)
    {
        var category = new Category { Name = request.Name.Trim(), Description = request.Description?.Trim() };
        await repo.AddAsync(category, ct);
        await repo.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(Guid id, CategoryRequest request, CancellationToken ct)
    {
        var category = await repo.GetByIdAsync(id, ct);
        if (category is null) return null;
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        await repo.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var category = await repo.GetByIdAsync(id, ct);
        if (category is null) return false;
        category.IsActive = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }

    private static CategoryResponse Map(Category c) => new(c.Id, c.Name, c.Description, c.IsActive);
}

public sealed class ExpenseService(IExpenseRepository expenses, ICategoryRepository categories, IBudgetRepository budgets) : IExpenseService
{
    public async Task<PagedResult<ExpenseResponse>> GetAsync(Guid userId, ExpenseQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var q = expenses.Query().Where(x => x.UserId == userId);

        if (query.CategoryId.HasValue) q = q.Where(x => x.CategoryId == query.CategoryId.Value);
        if (query.From.HasValue) q = q.Where(x => x.ExpenseDate >= query.From.Value.Date);
        if (query.To.HasValue) q = q.Where(x => x.ExpenseDate < query.To.Value.Date.AddDays(1));
        if (query.MinAmount.HasValue) q = q.Where(x => x.Amount >= query.MinAmount.Value);
        if (query.MaxAmount.HasValue) q = q.Where(x => x.Amount <= query.MaxAmount.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(x => x.Description.Contains(query.Search) || x.Category.Name.Contains(query.Search));

        q = (query.SortBy.ToLowerInvariant(), query.SortDirection.ToLowerInvariant()) switch
        {
            ("amount", "asc") => q.OrderBy(x => x.Amount),
            ("amount", _) => q.OrderByDescending(x => x.Amount),
            ("category", "asc") => q.OrderBy(x => x.Category.Name),
            ("category", _) => q.OrderByDescending(x => x.Category.Name),
            ("created", "asc") => q.OrderBy(x => x.CreatedAtUtc),
            ("created", _) => q.OrderByDescending(x => x.CreatedAtUtc),
            ("date", "asc") => q.OrderBy(x => x.ExpenseDate),
            _ => q.OrderByDescending(x => x.ExpenseDate)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * size).Take(size).Select(x =>
            new ExpenseResponse(x.Id, x.CategoryId, x.Category.Name, x.Amount, x.Description, x.ExpenseDate, x.PaymentMethod, x.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<ExpenseResponse>(items, page, size, total, (int)Math.Ceiling(total / (double)size));
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct) =>
        (await expenses.GetByIdAsync(id, userId, ct)) is { } e ? Map(e) : null;

    public async Task<ExpenseResponse> CreateAsync(Guid userId, ExpenseRequest request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        if (!category.IsActive) throw new InvalidOperationException("Category is inactive.");

        var expense = new Expense
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            ExpenseDate = request.ExpenseDate,
            PaymentMethod = request.PaymentMethod?.Trim()
        };

        await expenses.AddAsync(expense, ct);
        await expenses.SaveChangesAsync(ct);
        expense.Category = category;
        return Map(expense);
    }

    public async Task<ExpenseResponse?> UpdateAsync(Guid id, Guid userId, ExpenseRequest request, CancellationToken ct)
    {
        var expense = await expenses.GetByIdAsync(id, userId, ct);
        if (expense is null) return null;
        var category = await categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        if (!category.IsActive) throw new InvalidOperationException("Category is inactive.");

        expense.CategoryId = request.CategoryId;
        expense.Category = category;
        expense.Amount = request.Amount;
        expense.Description = request.Description.Trim();
        expense.ExpenseDate = request.ExpenseDate;
        expense.PaymentMethod = request.PaymentMethod?.Trim();
        await expenses.SaveChangesAsync(ct);
        return Map(expense);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var expense = await expenses.GetByIdAsync(id, userId, ct);
        if (expense is null) return false;
        expenses.Remove(expense);
        await expenses.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SpendingSummary> GetSummaryAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        var spent = await expenses.Query().Where(x => x.UserId == userId && x.ExpenseDate >= from && x.ExpenseDate < to)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;

        var budget = await budgets.Query().Where(x => x.UserId == userId && x.Year == year && x.Month == month)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;

        var remaining = budget - spent;
        var usage = budget <= 0 ? 0 : Math.Round(spent / budget * 100, 2);
        return new SpendingSummary(spent, budget, remaining, usage);
    }

    private static ExpenseResponse Map(Expense e) =>
        new(e.Id, e.CategoryId, e.Category.Name, e.Amount, e.Description, e.ExpenseDate, e.PaymentMethod, e.CreatedAtUtc);
}

public sealed class BudgetService(IBudgetRepository budgets, ICategoryRepository categories, IExpenseRepository expenses) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetResponse>> GetAsync(Guid userId, int? year, int? month, CancellationToken ct)
    {
        var q = budgets.Query().Where(x => x.UserId == userId);
        if (year.HasValue) q = q.Where(x => x.Year == year.Value);
        if (month.HasValue) q = q.Where(x => x.Month == month.Value);
        var list = await q.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync(ct);
        var result = new List<BudgetResponse>();

        foreach (var b in list)
        {
            var from = new DateTime(b.Year, b.Month, 1);
            var to = from.AddMonths(1);
            var spent = await expenses.Query().Where(x => x.UserId == userId && x.CategoryId == b.CategoryId &&
                x.ExpenseDate >= from && x.ExpenseDate < to).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            result.Add(Map(b, spent));
        }
        return result;
    }

    public async Task<BudgetResponse> CreateAsync(Guid userId, BudgetRequest request, CancellationToken ct)
    {
        _ = await categories.GetByIdAsync(request.CategoryId, ct) ?? throw new KeyNotFoundException("Category not found.");
        if (await budgets.Query().AnyAsync(x => x.UserId == userId && x.CategoryId == request.CategoryId && x.Year == request.Year && x.Month == request.Month, ct))
            throw new InvalidOperationException("A budget already exists for this category and month.");

        var b = new Budget { UserId = userId, CategoryId = request.CategoryId, Amount = request.Amount, Year = request.Year, Month = request.Month };
        await budgets.AddAsync(b, ct);
        await budgets.SaveChangesAsync(ct);
        b.Category = (await categories.GetByIdAsync(request.CategoryId, ct))!;
        return Map(b, 0);
    }

    public async Task<BudgetResponse?> UpdateAsync(Guid id, Guid userId, BudgetRequest request, CancellationToken ct)
    {
        var b = await budgets.GetByIdAsync(id, userId, ct);
        if (b is null) return null;
        _ = await categories.GetByIdAsync(request.CategoryId, ct) ?? throw new KeyNotFoundException("Category not found.");
        b.CategoryId = request.CategoryId; b.Amount = request.Amount; b.Year = request.Year; b.Month = request.Month;
        b.Category = (await categories.GetByIdAsync(request.CategoryId, ct))!;
        await budgets.SaveChangesAsync(ct);
        return Map(b, 0);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var b = await budgets.GetByIdAsync(id, userId, ct);
        if (b is null) return false;
        budgets.Remove(b);
        await budgets.SaveChangesAsync(ct);
        return true;
    }

    private static BudgetResponse Map(Budget b, decimal spent)
    {
        var remaining = b.Amount - spent;
        var usage = b.Amount <= 0 ? 0 : Math.Round(spent / b.Amount * 100, 2);
        return new BudgetResponse(b.Id, b.CategoryId, b.Category.Name, b.Amount, b.Year, b.Month, spent, remaining, usage);
    }
}
