using System.Security.Claims;
using BudgetTracker.Data;
using BudgetTracker.Models;
using BudgetTracker.Security;
using BudgetTracker.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly bool _registrationEnabled;

    public AccountController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _registrationEnabled = configuration.GetValue("Registration:Enabled", true);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["RegistrationEnabled"] = _registrationEnabled;
        return View(new LoginRequest { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Credentials)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        ViewData["RegistrationEnabled"] = _registrationEnabled;

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var username = request.Username.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        // Same message either way, so the form never reveals which half was wrong.
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Incorrect username or password.");
            return View(request);
        }

        await SignIn(user, request.RememberMe);

        return RedirectToLocalOrDashboard(request.ReturnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        // 404 rather than a "registration is closed" page, so a closed instance does
        // not advertise that the endpoint exists.
        if (!_registrationEnabled)
        {
            return NotFound();
        }

        return View(new RegisterRequest());
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Credentials)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!_registrationEnabled)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var username = request.Username.Trim();

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            ModelState.AddModelError(nameof(request.Username), "That username is already taken.");
            return View(request);
        }

        var isFirstUser = !await _context.Users.AnyAsync();

        var user = new AppUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (isFirstUser)
        {
            // Budgets that predate user accounts have no owner. Hand them to the very
            // first person to register so existing data is not stranded.
            var unowned = await _context.BudgetMonths.Where(b => b.UserId == null).ToListAsync();

            foreach (var budget in unowned)
            {
                budget.UserId = user.Id;
            }

            if (unowned.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        await SignIn(user, isPersistent: true);

        TempData["StatusMessage"] = $"Welcome, {user.Username}. Your account is ready.";
        return RedirectToAction("Index", "Budget");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignIn(AppUser user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = isPersistent });
    }

    private IActionResult RedirectToLocalOrDashboard(string? returnUrl)
    {
        // Url.IsLocalUrl keeps a crafted returnUrl from bouncing the user to another site.
        return Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Budget");
    }
}
