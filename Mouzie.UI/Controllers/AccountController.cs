using Microsoft.AspNetCore.Mvc;
using Mouzie.Data.Models.ViewModels;
using Mouzie.Data.Repositories.Interfaces;
using Mouzie.UI.Filters;
using Mouzie.UI.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace Mouzie.UI.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ISessionService _session;
    private readonly ILogger<AccountController> _log;

    public AccountController(IUserRepository users, IPasswordHasher hasher,
                             ISessionService session, ILogger<AccountController> log)
    {
        _users = users;
        _hasher = hasher;
        _session = session;
        _log = log;
    }

    // ── Login ─────────────────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("auth")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_session.IsAuthenticated)
            return RedirectToAction("Index", "Admin");

        return View(new LoginVM { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var username = vm.Username.Trim().ToLowerInvariant();
        var user = await _users.GetByUsernameAsync(username);

        var ok = user is not null && _hasher.Verify(vm.Password, user.PasswordHash);
        if (!ok)
        {
            _log.LogWarning("Failed login attempt for {Username}", username);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(vm);
        }

        _session.SignIn(user!.UserId, user.Username);
        await _users.UpdateLastLoginAsync(user.UserId, DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        return RedirectToAction("Index", "Admin");
    }

    // ── Logout ────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _session.SignOut();
        return RedirectToAction("Index", "Home");
    }

    // ── Change Password ──────────────────────────────────────────
    [HttpGet, AdminAuth]
    public IActionResult ChangePassword() => View(new ChangePasswordVM());

    [HttpPost, ValidateAntiForgeryToken, AdminAuth]
    public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _users.GetByUsernameAsync(_session.Username!);
        if (user is null || !_hasher.Verify(vm.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is incorrect.");
            return View(vm);
        }

        await _users.UpdatePasswordAsync(user.UserId, _hasher.Hash(vm.NewPassword));

        TempData["Success"] = "Password updated. Please sign in again.";
        _session.SignOut();
        return RedirectToAction(nameof(Login));
    }
}