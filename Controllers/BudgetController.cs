using BudgetTracker.Data;
using BudgetTracker.Models;
using BudgetTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class BudgetController : Controller
{
    private readonly ApplicationDbContext _context;

    public BudgetController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, int? budgetMonthId)
    {
        var currentDate = DateTime.Today;
        var selectedYear = year ?? currentDate.Year;
        var selectedMonth = month ?? currentDate.Month;

        if (budgetMonthId.HasValue)
        {
            var selectedBudget = await _context.BudgetMonths.FirstOrDefaultAsync(b => b.Id == budgetMonthId.Value);
            if (selectedBudget is not null)
            {
                selectedYear = selectedBudget.Year;
                selectedMonth = selectedBudget.Month;
            }
        }

        return View(await BuildDashboardViewModel(selectedYear, selectedMonth, budgetMonthId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBudget(int year, int month, string name, decimal totalBudget)
    {
        if (!IsValidMonth(year, month))
        {
            ModelState.AddModelError(string.Empty, "Choose a valid month before creating a budget.");
            return View(nameof(Index), await BuildDashboardViewModel(DateTime.Today.Year, DateTime.Today.Month));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Budget name is required.");
            return View(nameof(Index), await BuildDashboardViewModel(year, month));
        }

        if (totalBudget < 0)
        {
            ModelState.AddModelError(string.Empty, "Budget amount cannot be negative.");
            return View(nameof(Index), await BuildDashboardViewModel(year, month));
        }

        var budgetMonth = new BudgetMonth
        {
            Year = year,
            Month = month,
            Name = name.Trim(),
            TotalBudget = totalBudget
        };

        _context.BudgetMonths.Add(budgetMonth);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Budget created.";
        return RedirectToAction(nameof(Index), new { year, month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMonthlyBudget(int budgetMonthId, string name, decimal totalBudget)
    {
        var budgetMonth = await _context.BudgetMonths.FirstOrDefaultAsync(b => b.Id == budgetMonthId);

        if (budgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Selected budget was not found.");
            return View(nameof(Index), await BuildDashboardViewModel(DateTime.Today.Year, DateTime.Today.Month));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Budget name is required.");
            return View(nameof(Index), await BuildDashboardViewModel(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id));
        }

        if (totalBudget < 0)
        {
            ModelState.AddModelError(string.Empty, "Budget amount cannot be negative.");
            return View(nameof(Index), await BuildDashboardViewModel(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id));
        }

        budgetMonth.Name = name.Trim();
        budgetMonth.TotalBudget = totalBudget;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Budget updated.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMonthlyBudget(int budgetMonthId)
    {
        var budgetMonth = await _context.BudgetMonths.FirstOrDefaultAsync(b => b.Id == budgetMonthId);

        if (budgetMonth is null)
        {
            TempData["StatusMessage"] = "The budget was not found.";
            return RedirectToAction(nameof(Index));
        }

        var year = budgetMonth.Year;
        var month = budgetMonth.Month;

        _context.BudgetMonths.Remove(budgetMonth);
        await _context.SaveChangesAsync();

        var fallbackBudget = await _context.BudgetMonths
            .Where(b => b.Year == year && b.Month == month)
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync();

        TempData["StatusMessage"] = "Budget deleted.";
        return fallbackBudget is null
            ? RedirectToAction(nameof(Index), new { year, month })
            : RedirectToAction(nameof(Index), new { year, month, budgetMonthId = fallbackBudget.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int budgetMonthId, string name, decimal budgetAmount)
    {
        var budgetMonth = await _context.BudgetMonths.FindAsync(budgetMonthId);

        if (budgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Save or create the selected budget before adding categories.");
            return View(nameof(Index), await BuildDashboardViewModel(DateTime.Today.Year, DateTime.Today.Month));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Category name is required.");
            return View(nameof(Index), await BuildDashboardViewModel(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id));
        }

        if (budgetAmount < 0)
        {
            ModelState.AddModelError(string.Empty, "Category budget cannot be negative.");
            return View(nameof(Index), await BuildDashboardViewModel(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id));
        }

        _context.CategoryBudgets.Add(new CategoryBudget
        {
            BudgetMonthId = budgetMonthId,
            Name = name.Trim(),
            BudgetAmount = budgetAmount
        });

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Category added.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(int categoryBudgetId, string name, decimal budgetAmount)
    {
        var category = await _context.CategoryBudgets
            .Include(c => c.BudgetMonth)
            .FirstOrDefaultAsync(c => c.Id == categoryBudgetId);

        if (category is null)
        {
            ModelState.AddModelError(string.Empty, "Category not found.");
            return View(nameof(Index), await BuildDashboardViewModel(DateTime.Today.Year, DateTime.Today.Month));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Category name is required.");
            return View(nameof(Index), await BuildDashboardViewModel(category.BudgetMonth!.Year, category.BudgetMonth.Month, category.BudgetMonth.Id));
        }

        if (budgetAmount < 0)
        {
            ModelState.AddModelError(string.Empty, "Category budget cannot be negative.");
            return View(nameof(Index), await BuildDashboardViewModel(category.BudgetMonth!.Year, category.BudgetMonth.Month, category.BudgetMonth.Id));
        }

        category.Name = name.Trim();
        category.BudgetAmount = budgetAmount;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { year = category.BudgetMonth?.Year, month = category.BudgetMonth?.Month, budgetMonthId = category.BudgetMonth?.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryBudgetId)
    {
        var category = await _context.CategoryBudgets
            .Include(c => c.BudgetMonth)
            .FirstOrDefaultAsync(c => c.Id == categoryBudgetId);

        if (category is null)
        {
            TempData["StatusMessage"] = "The category was not found.";
            return RedirectToAction(nameof(Index));
        }

        var year = category.BudgetMonth?.Year ?? DateTime.Today.Year;
        var month = category.BudgetMonth?.Month ?? DateTime.Today.Month;
        var budgetMonthId = category.BudgetMonth?.Id;

        _context.CategoryBudgets.Remove(category);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Category deleted.";
        return RedirectToAction(nameof(Index), new { year, month, budgetMonthId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(int categoryBudgetId, string description, decimal amount, DateTime purchasedAt)
    {
        var category = await _context.CategoryBudgets
            .Include(c => c.BudgetMonth)
            .FirstOrDefaultAsync(c => c.Id == categoryBudgetId);

        if (category is null)
        {
            ModelState.AddModelError(string.Empty, "Category not found.");
            return View(nameof(Index), await BuildDashboardViewModel(DateTime.Today.Year, DateTime.Today.Month));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ModelState.AddModelError(string.Empty, "Expense description is required.");
            return View(nameof(Index), await BuildDashboardViewModel(category.BudgetMonth!.Year, category.BudgetMonth.Month, category.BudgetMonth.Id));
        }

        if (amount < 0)
        {
            ModelState.AddModelError(string.Empty, "Expense amount cannot be negative.");
            return View(nameof(Index), await BuildDashboardViewModel(category.BudgetMonth!.Year, category.BudgetMonth.Month, category.BudgetMonth.Id));
        }

        _context.Expenses.Add(new Expense
        {
            CategoryBudgetId = categoryBudgetId,
            Description = description.Trim(),
            Amount = amount,
            PurchasedAt = purchasedAt == default ? DateTime.Today : purchasedAt
        });

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Expense added.";
        return RedirectToAction(nameof(Index), new { year = category.BudgetMonth?.Year, month = category.BudgetMonth?.Month, budgetMonthId = category.BudgetMonth?.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveExpense(int expenseId, int categoryBudgetId, string description, decimal amount, DateTime purchasedAt)
    {
        var expense = await _context.Expenses
            .Include(e => e.CategoryBudget)
            .ThenInclude(c => c!.BudgetMonth)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense is null)
        {
            ModelState.AddModelError(string.Empty, "Expense not found.");
            return View(nameof(Index), await BuildDashboardViewModel(DateTime.Today.Year, DateTime.Today.Month));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ModelState.AddModelError(string.Empty, "Expense description is required.");
            return View(nameof(Index), await BuildDashboardViewModel(expense.CategoryBudget!.BudgetMonth!.Year, expense.CategoryBudget.BudgetMonth.Month, expense.CategoryBudget.BudgetMonth.Id));
        }

        if (amount < 0)
        {
            ModelState.AddModelError(string.Empty, "Expense amount cannot be negative.");
            return View(nameof(Index), await BuildDashboardViewModel(expense.CategoryBudget!.BudgetMonth!.Year, expense.CategoryBudget.BudgetMonth.Month, expense.CategoryBudget.BudgetMonth.Id));
        }

        expense.Description = description.Trim();
        expense.Amount = amount;
        expense.PurchasedAt = purchasedAt == default ? expense.PurchasedAt : purchasedAt;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Expense updated.";
        return RedirectToAction(nameof(Index), new { year = expense.CategoryBudget?.BudgetMonth?.Year, month = expense.CategoryBudget?.BudgetMonth?.Month, budgetMonthId = expense.CategoryBudget?.BudgetMonth?.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExpense(int expenseId)
    {
        var expense = await _context.Expenses
            .Include(e => e.CategoryBudget)
            .ThenInclude(c => c!.BudgetMonth)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense is null)
        {
            TempData["StatusMessage"] = "The expense was not found.";
            return RedirectToAction(nameof(Index));
        }

        var year = expense.CategoryBudget?.BudgetMonth?.Year ?? DateTime.Today.Year;
        var month = expense.CategoryBudget?.BudgetMonth?.Month ?? DateTime.Today.Month;
        var budgetMonthId = expense.CategoryBudget?.BudgetMonth?.Id;

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Expense deleted.";
        return RedirectToAction(nameof(Index), new { year, month, budgetMonthId });
    }

    private async Task<BudgetDashboardViewModel> BuildDashboardViewModel(int year, int month, int? budgetMonthId = null)
    {
        var availableBudgets = await _context.BudgetMonths
            .Where(b => b.Year == year && b.Month == month)
            .OrderByDescending(b => b.Id)
            .Select(b => new BudgetMonthSummaryViewModel
            {
                Id = b.Id,
                Name = b.Name,
                TotalBudget = b.TotalBudget,
                IsSelected = budgetMonthId.HasValue && b.Id == budgetMonthId.Value
            })
            .ToListAsync();

        var selectedBudget = budgetMonthId.HasValue
            ? await _context.BudgetMonths
                .Include(b => b.Categories)
                .ThenInclude(c => c.Expenses)
                .FirstOrDefaultAsync(b => b.Id == budgetMonthId.Value)
            : null;

        if (selectedBudget is null)
        {
            selectedBudget = await _context.BudgetMonths
                .Include(b => b.Categories)
                .ThenInclude(c => c.Expenses)
                .Where(b => b.Year == year && b.Month == month)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();
        }

        return new BudgetDashboardViewModel
        {
            BudgetMonthId = selectedBudget?.Id,
            BudgetName = selectedBudget?.Name ?? string.Empty,
            Year = year,
            Month = month,
            TotalBudget = selectedBudget?.TotalBudget ?? 0,
            TotalSpent = selectedBudget?.Categories.Sum(c => c.Expenses.Sum(e => e.Amount)) ?? 0,
            AvailableBudgets = availableBudgets,
            Categories = selectedBudget?.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryBudgetViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    BudgetAmount = c.BudgetAmount,
                    Spent = c.Expenses.Sum(e => e.Amount),
                    Expenses = c.Expenses
                        .OrderByDescending(e => e.PurchasedAt)
                        .Select(e => new ExpenseViewModel
                        {
                            Id = e.Id,
                            Description = e.Description,
                            Amount = e.Amount,
                            PurchasedAt = e.PurchasedAt
                        })
                        .ToList()
                })
                .ToList() ?? new List<CategoryBudgetViewModel>()
        };
    }

    private static bool IsValidMonth(int year, int month)
    {
        return year >= 1 && month is >= 1 and <= 12;
    }
}