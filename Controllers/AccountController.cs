using System.Security.Claims;
using LeaveManager.Models;
using LeaveManager.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManager.Controllers;

public sealed class AccountController(DemoUsers users) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login() => View(new LoginForm());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = users.Authenticate(form.Email, form.Password);
        if (user is null)
        {
            ModelState.AddModelError("", "Email or password is incorrect.");
            return View(form);
        }
        Claim[] claims = [new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name), new(ClaimTypes.Role, user.Role)];
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        return RedirectToAction("Index", "Leave");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Denied() => StatusCode(StatusCodes.Status403Forbidden,
        "You do not have permission to perform this action.");
}
