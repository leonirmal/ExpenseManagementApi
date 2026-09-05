using ExpenseManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManagement.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300);
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(300).IsRequired();
            e.Property(x => x.PaymentMethod).HasMaxLength(50);
            e.HasOne(x => x.User).WithMany(x => x.Expenses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(x => x.Expenses).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.ExpenseDate });
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.UserId, x.CategoryId, x.Year, x.Month }).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.Budgets).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Food", Description = "Groceries and meals" },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Transport", Description = "Fuel, public transport and cabs" },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Bills", Description = "Utilities and subscriptions" },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Shopping", Description = "Personal and household shopping" },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Other", Description = "Miscellaneous expenses" }
        );
    }
}
