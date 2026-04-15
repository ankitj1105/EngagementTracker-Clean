using EngagementTracker.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

[Route("Quiz")]
public class QuizController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return RedirectToAction("Login", "Auth");
        ViewBag.Role = SessionHelper.GetRole(HttpContext.Session);
        ViewBag.Name = SessionHelper.GetName(HttpContext.Session);
        ViewBag.Uid  = SessionHelper.GetUid(HttpContext.Session);
        return View();
    }
}