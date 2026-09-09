using BudgetTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BudgetMonth> BudgetMonths => Set<BudgetMonth>();
    public DbSet<CategoryBudget> CategoryBudgets => Set<CategoryBudget>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<BudgetMonth>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BudgetMonth>()
            .HasMany(m => m.Categories)
            .WithOne(c => c.BudgetMonth!)
            .HasForeignKey(c => c.BudgetMonthId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CategoryBudget>()
            .HasMany(c => c.Expenses)
            .WithOne(e => e.CategoryBudget!)
            .HasForeignKey(e => e.CategoryBudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}