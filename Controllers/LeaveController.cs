using System.Security.Claims;
using LeaveManager.Models;
using LeaveManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManager.Controllers;

[Authorize]
public sealed class LeaveController(LeaveBook book) : Controller
{
    private string EmployeeId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public IActionResult Index() => View(book.Read(EmployeeId, User.IsInRole("Manager")));

    [HttpGet, Authorize(Roles = "Employee")]
    public IActionResult Create() => View(new RequestForm());

    [HttpPost, Authorize(Roles = "Employee"), ValidateAntiForgeryToken]
    public IActionResult Create(RequestForm form)
    {
        if (!ModelState.IsValid) return View(form);
        // Identity always comes from the signed cookie, never from posted fields.
        var error = book.Submit(EmployeeId, User.Identity!.Name!, form);
        if (error is not null)
        {
            ModelState.AddModelError("", error);
            return View(form);
        }
        TempData["Message"] = "Leave request submitted. Days are reserved while pending.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public IActionResult Decide(int id, string decision)
    {
        if (decision is not "approve" and not "reject")
            return BadRequest("Choose approve or reject.");
        var error = book.Decide(id, decision == "approve", User.Identity!.Name!);
        if (error is not null) return Conflict(error);
        TempData["Message"] = decision == "approve" ? "Request approved." : "Request rejected. Reserved days released.";
        return RedirectToAction(nameof(Index));
    }
}
