using System.Security.Claims;
using BudgetTracker.Data;
using BudgetTracker.Models;
using BudgetTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

[Authorize]
public class BudgetController : Controller
{
    private const int MinYear = 2000;
    private const int MaxYear = 2100;

    private readonly ApplicationDbContext _context;

    public BudgetController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, int? budgetMonthId)
    {
        var userId = CurrentUserId;
        var selectedYear = ClampYear(year);
        var selectedMonth = ClampMonth(month);

        if (budgetMonthId.HasValue)
        {
            var selectedBudget = await _context.BudgetMonths
                .FirstOrDefaultAsync(b => b.Id == budgetMonthId.Value && b.UserId == userId);

            if (selectedBudget is not null)
            {
                selectedYear = selectedBudget.Year;
                selectedMonth = selectedBudget.Month;
            }
            else
            {
                // Someone else's budget id, or one that no longer exists: fall back to
                // this user's own data rather than acknowledging that the id exists.
                budgetMonthId = null;
            }
        }

        return View(await BuildDashboardViewModel(selectedYear, selectedMonth, budgetMonthId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBudget(CreateBudgetRequest request)
    {
        var userId = CurrentUserId;

        if (!ModelState.IsValid)
        {
            return await IndexWithErrors(request.Year, request.Month);
        }

        var budgetMonth = new BudgetMonth
        {
            UserId = userId,
            Year = request.Year,
            Month = request.Month,
            Name = request.Name.Trim(),
            TotalBudget = request.TotalBudget
        };

        var copiedCategoryCount = 0;

        if (request.CopyCategoriesFromPreviousMonth)
        {
            var previousMonth = new DateTime(request.Year, request.Month, 1).AddMonths(-1);

            var sourceBudget = await _context.BudgetMonths
                .Include(b => b.Categories)
                .Where(b => b.UserId == userId && b.Year == previousMonth.Year && b.Month == previousMonth.Month)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            if (sourceBudget is not null)
            {
                budgetMonth.Categories = sourceBudget.Categories
                    .Select(c => new CategoryBudget { Name = c.Name, BudgetAmount = c.BudgetAmount })
                    .ToList();

                copiedCategoryCount = budgetMonth.Categories.Count;
            }
        }

        _context.BudgetMonths.Add(budgetMonth);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = copiedCategoryCount > 0
            ? $"Budget created with {copiedCategoryCount} category(s) copied from the previous month."
            : "Budget created.";

        return RedirectToAction(nameof(Index), new { request.Year, request.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMonthlyBudget(UpdateBudgetRequest request)
    {
        var userId = CurrentUserId;

        var budgetMonth = await _context.BudgetMonths
            .FirstOrDefaultAsync(b => b.Id == request.BudgetMonthId && b.UserId == userId);

        if (budgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Selected budget was not found.");
            return await IndexWithErrors(DateTime.Today.Year, DateTime.Today.Month);
        }

        if (!ModelState.IsValid)
        {
            return await IndexWithErrors(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id);
        }

        budgetMonth.Name = request.Name.Trim();
        budgetMonth.TotalBudget = request.TotalBudget;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Budget updated.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMonthlyBudget(int budgetMonthId)
    {
        var userId = CurrentUserId;

        var budgetMonth = await _context.BudgetMonths
            .FirstOrDefaultAsync(b => b.Id == budgetMonthId && b.UserId == userId);

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
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync();

        TempData["StatusMessage"] = "Budget deleted.";
        return fallbackBudget is null
            ? RedirectToAction(nameof(Index), new { year, month })
            : RedirectToAction(nameof(Index), new { year, month, budgetMonthId = fallbackBudget.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request)
    {
        var userId = CurrentUserId;

        var budgetMonth = await _context.BudgetMonths
            .FirstOrDefaultAsync(b => b.Id == request.BudgetMonthId && b.UserId == userId);

        if (budgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Save or create the selected budget before adding categories.");
            return await IndexWithErrors(DateTime.Today.Year, DateTime.Today.Month);
        }

        if (!ModelState.IsValid)
        {
            return await IndexWithErrors(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id);
        }

        _context.CategoryBudgets.Add(new CategoryBudget
        {
            BudgetMonthId = budgetMonth.Id,
            Name = request.Name.Trim(),
            BudgetAmount = request.BudgetAmount
        });

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Category added.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(SaveCategoryRequest request)
    {
        var category = await FindOwnedCategory(request.CategoryBudgetId);

        if (category?.BudgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Category not found.");
            return await IndexWithErrors(DateTime.Today.Year, DateTime.Today.Month);
        }

        var budgetMonth = category.BudgetMonth;

        if (!ModelState.IsValid)
        {
            return await IndexWithErrors(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id);
        }

        category.Name = request.Name.Trim();
        category.BudgetAmount = request.BudgetAmount;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryBudgetId)
    {
        var category = await FindOwnedCategory(categoryBudgetId);

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
    public async Task<IActionResult> AddExpense(AddExpenseRequest request)
    {
        var category = await FindOwnedCategory(request.CategoryBudgetId);

        if (category?.BudgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Category not found.");
            return await IndexWithErrors(DateTime.Today.Year, DateTime.Today.Month);
        }

        var budgetMonth = category.BudgetMonth;

        if (!ModelState.IsValid)
        {
            return await IndexWithErrors(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id);
        }

        _context.Expenses.Add(new Expense
        {
            CategoryBudgetId = category.Id,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            PurchasedAt = request.PurchasedAt ?? DateTime.Today
        });

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Expense added.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveExpense(SaveExpenseRequest request)
    {
        var expense = await FindOwnedExpense(request.ExpenseId);

        if (expense?.CategoryBudget?.BudgetMonth is null)
        {
            ModelState.AddModelError(string.Empty, "Expense not found.");
            return await IndexWithErrors(DateTime.Today.Year, DateTime.Today.Month);
        }

        var budgetMonth = expense.CategoryBudget.BudgetMonth;

        if (!ModelState.IsValid)
        {
            return await IndexWithErrors(budgetMonth.Year, budgetMonth.Month, budgetMonth.Id);
        }

        expense.Description = request.Description.Trim();
        expense.Amount = request.Amount;
        expense.PurchasedAt = request.PurchasedAt ?? expense.PurchasedAt;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Expense updated.";
        return RedirectToAction(nameof(Index), new { year = budgetMonth.Year, month = budgetMonth.Month, budgetMonthId = budgetMonth.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExpense(int expenseId)
    {
        var expense = await FindOwnedExpense(expenseId);

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

    // Ownership is part of the lookup, so another user's id simply comes back null
    // and every caller then reports "not found".
    private Task<CategoryBudget?> FindOwnedCategory(int categoryBudgetId)
    {
        var userId = CurrentUserId;

        return _context.CategoryBudgets
            .Include(c => c.BudgetMonth)
            .FirstOrDefaultAsync(c => c.Id == categoryBudgetId && c.BudgetMonth!.UserId == userId);
    }

    private Task<Expense?> FindOwnedExpense(int expenseId)
    {
        var userId = CurrentUserId;

        return _context.Expenses
            .Include(e => e.CategoryBudget)
            .ThenInclude(c => c!.BudgetMonth)
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.CategoryBudget!.BudgetMonth!.UserId == userId);
    }

    private async Task<IActionResult> IndexWithErrors(int year, int month, int? budgetMonthId = null)
    {
        return View(nameof(Index), await BuildDashboardViewModel(ClampYear(year), ClampMonth(month), budgetMonthId));
    }

    private async Task<BudgetDashboardViewModel> BuildDashboardViewModel(int year, int month, int? budgetMonthId = null)
    {
        var userId = CurrentUserId;

        var availableBudgets = await _context.BudgetMonths
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .OrderByDescending(b => b.Id)
            .Select(b => new BudgetMonthSummaryViewModel
            {
                Id = b.Id,
                Name = b.Name,
                TotalBudget = b.TotalBudget
            })
            .ToListAsync();

        var selectedBudget = budgetMonthId.HasValue
            ? await _context.BudgetMonths
                .Include(b => b.Categories)
                .ThenInclude(c => c.Expenses)
                .FirstOrDefaultAsync(b => b.Id == budgetMonthId.Value && b.UserId == userId)
            : null;

        if (selectedBudget is null)
        {
            selectedBudget = await _context.BudgetMonths
                .Include(b => b.Categories)
                .ThenInclude(c => c.Expenses)
                .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();
        }

        // Marked after the fallback resolves, so the sidebar highlights the budget
        // actually on screen even when the URL carries no explicit id.
        foreach (var summary in availableBudgets)
        {
            summary.IsSelected = summary.Id == selectedBudget?.Id;
        }

        var previousMonth = new DateTime(year, month, 1).AddMonths(-1);
        var hasPreviousMonthBudget = await _context.BudgetMonths
            .AnyAsync(b => b.UserId == userId && b.Year == previousMonth.Year && b.Month == previousMonth.Month);

        return new BudgetDashboardViewModel
        {
            BudgetMonthId = selectedBudget?.Id,
            BudgetName = selectedBudget?.Name ?? string.Empty,
            Year = year,
            Month = month,
            TotalBudget = selectedBudget?.TotalBudget ?? 0,
            TotalSpent = selectedBudget?.Categories.Sum(c => c.Expenses.Sum(e => e.Amount)) ?? 0,
            HasPreviousMonthBudget = hasPreviousMonthBudget,
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

    private static int ClampYear(int? year)
    {
        return year is >= MinYear and <= MaxYear ? year.Value : DateTime.Today.Year;
    }

    private static int ClampMonth(int? month)
    {
        return month is >= 1 and <= 12 ? month.Value : DateTime.Today.Month;
    }
}
