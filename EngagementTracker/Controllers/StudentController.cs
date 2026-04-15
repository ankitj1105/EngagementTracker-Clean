using EngagementTracker.Helpers;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;

namespace EngagementTracker.Controllers;

public class StudentController : Controller
{
    private readonly FirestoreDb _db;

    public StudentController(FirestoreDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return RedirectToAction("Login", "Auth");

        if (SessionHelper.GetRole(HttpContext.Session) != "student")
            return RedirectToAction("Dashboard", "Teacher");

        var uid = SessionHelper.GetUid(HttpContext.Session);
        ViewBag.Name = SessionHelper.GetName(HttpContext.Session);
        ViewBag.Uid  = uid;

        var section = SessionHelper.GetSection(HttpContext.Session);
        if (string.IsNullOrEmpty(section))
        {
            var userDoc = await _db.Collection("users").Document(uid).GetSnapshotAsync();
            if (userDoc.Exists && userDoc.ContainsField("section"))
            {
                section = userDoc.GetValue<string>("section");
                HttpContext.Session.SetString("section", section);
            }
        }
        ViewBag.Section = section;

        return View();
    }
}