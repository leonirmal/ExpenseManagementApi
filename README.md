# Expense Management API

Backend-focused portfolio REST API built with **ASP.NET Core 10, C#, EF Core, PostgreSQL/SQL Server, JWT, LINQ and Swagger/OpenAPI**.

## Architecture

```text
HTTP / Swagger
     |
Controllers
     |
Services / business rules
     |
Repository interfaces
     |
EF Core DbContext
     |
PostgreSQL or Azure SQL
```

Projects:
- `ExpenseManagement.Api` - HTTP layer, Swagger, JWT, validation and middleware
- `ExpenseManagement.Core` - entities, DTOs and interfaces
- `ExpenseManagement.Infrastructure` - EF Core, repositories and services
- `ExpenseManagement.Tests` - unit-test project starter

## Features

- JWT authentication
- Role-based authorization (`User`, `Admin`)
- Expense CRUD
- Category CRUD (write operations require Admin)
- Monthly budgets
- Spending summary
- Filtering, sorting and pagination
- FluentValidation
- Global exception handling
- EF Core with PostgreSQL or SQL Server
- Swagger JWT authorization
- Health endpoint
- Azure App Service configuration through environment variables
- No secrets required in source control

## Run locally with PostgreSQL

Prerequisites:
- .NET 10 SDK
- Docker Desktop

```bash
docker compose -f docker-compose.postgres.yml up -d
dotnet restore
dotnet run --project ExpenseManagementApi
```

Open:

`https://localhost:7168/swagger`

The default local PostgreSQL configuration is already in `appsettings.json`.

## Local SQL Server option

Change:

```json
"DatabaseProvider": "SqlServer"
```

and use:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=expense_management;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
}
```

For development, prefer environment variables or .NET user secrets rather than committing passwords.

## Environment variables

ASP.NET Core environment variables override appsettings values.

PostgreSQL:

```bash
export DatabaseProvider=PostgreSQL
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=expense_management;Username=postgres;Password=postgres'
export Jwt__Key='replace-with-a-long-random-secret-at-least-32-characters'
export Jwt__Issuer='ExpenseManagementApi'
export Jwt__Audience='ExpenseManagementClients'
```

SQL Server:

```bash
export DatabaseProvider=SqlServer
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=expense_management;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True'
```

## Azure App Service + Azure SQL

This project is intentionally configured so Azure App Service can override `appsettings.json` without code changes.

Set these App Service > Environment variables > App settings:

```text
DatabaseProvider = SqlServer
ConnectionStrings__DefaultConnection = Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
Jwt__Key = <long-random-production-secret>
Jwt__Issuer = ExpenseManagementApi
Jwt__Audience = ExpenseManagementClients
Jwt__ExpiresMinutes = 60
```

The application calls `EnsureCreatedAsync()` at startup, so a new empty database gets the tables automatically. For a larger production system, replace this with reviewed EF Core migrations.

### Azure free-cost approach

Microsoft currently lists an App Service Free plan and an Azure SQL Database free offer. The Azure SQL free offer provides 100,000 vCore seconds, 32 GB data and 32 GB backup per database per month; configure the database to auto-pause when the free limit is reached if you want to avoid overage charges. Check the current Azure portal offer before deployment.

## API flow in Swagger

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. Copy `accessToken`
4. Click **Authorize** in Swagger
5. Enter `Bearer <token>`
6. Use `/api/categories`, `/api/expenses`, `/api/budgets`

Example expense:

```json
{
  "categoryId": "10000000-0000-0000-0000-000000000001",
  "amount": 450.50,
  "description": "Weekly groceries",
  "expenseDate": "2026-09-05T00:00:00Z",
  "paymentMethod": "Card"
}
```

Example filtering:

```text
GET /api/expenses?page=1&pageSize=10&categoryId=<guid>&from=2026-09-01&to=2026-09-30&minAmount=100&maxAmount=2000&search=food&sortBy=amount&sortDirection=desc
```

## Important portfolio note

The Swagger page is the primary live demo. A strong GitHub README should additionally show:
- architecture diagram
- API endpoint table
- sample requests/responses
- Azure deployment URL
- database schema screenshot
- technologies and design decisions
- test results

Do not commit production secrets.
