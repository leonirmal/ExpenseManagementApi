using System.Security.Claims;
using ExpenseManagement.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Api.Controllers;

[ApiController, Route("api/auth"), Tags("Authentication")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, ct);
        return result is null ? Conflict(new { message = "Email is already registered." }) : Created("", result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result is null ? Unauthorized(new { message = "Invalid email or password." }) : Ok(result);
    }
}

[ApiController, Authorize, Route("api/categories"), Tags("Categories")]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CategoryRequest request, CancellationToken ct) =>
        Created("", await service.CreateAsync(request, ct));

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, CategoryRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, request, ct)) is { } x ? Ok(x) : NotFound();

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}

[ApiController, Authorize, Route("api/expenses"), Tags("Expenses")]
public sealed class ExpensesController(IExpenseService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ExpenseQuery query, CancellationToken ct) => Ok(await service.GetAsync(UserId, query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, UserId, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(ExpenseRequest request, CancellationToken ct) =>
        Created("", await service.CreateAsync(UserId, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ExpenseRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, UserId, request, ct)) is { } x ? Ok(x) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await service.DeleteAsync(id, UserId, ct) ? NoContent() : NotFound();

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(await service.GetSummaryAsync(UserId, year, month, ct));
}

[ApiController, Authorize, Route("api/budgets"), Tags("Budgets")]
public sealed class BudgetsController(IBudgetService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct) =>
        Ok(await service.GetAsync(UserId, year, month, ct));

    [HttpPost]
    public async Task<IActionResult> Create(BudgetRequest request, CancellationToken ct) =>
        Created("", await service.CreateAsync(UserId, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BudgetRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, UserId, request, ct)) is { } x ? Ok(x) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await service.DeleteAsync(id, UserId, ct) ? NoContent() : NotFound();
}

[ApiController, Route("api/admin"), Tags("Admin")]
public sealed class AdminController(IUserRepository users) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "Healthy", service = "Expense Management API", utc = DateTime.UtcNow });

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(new { user.Id, user.FullName, user.Email, role = user.Role.ToString(), user.CreatedAtUtc });
    }
}
