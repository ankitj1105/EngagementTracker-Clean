using EngagementTracker.Helpers;
using EngagementTracker.Services;
using EngagementTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

public class MarksController : Controller
{
    private readonly MarksService _svc;

    public MarksController(MarksService svc)
    {
        _svc = svc;
    }

    // Teacher saves marks
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] MarksRecord req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        if (SessionHelper.GetRole(HttpContext.Session) != "teacher")
            return Forbid();

        var ok = await _svc.SaveMarksAsync(
            req.StudentUid,
            req.SubjectId,
            req.SubjectName,
            req.ExamType,
            req.Score,
            req.MaxScore
        );

        return ok ? Ok() : BadRequest(new { error = "Failed to save marks" });
    }

    [HttpPost]
    public async Task<IActionResult> SaveBulk([FromBody] List<MarksRecord> req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        if (SessionHelper.GetRole(HttpContext.Session) != "teacher")
            return Forbid();

        var ok = await _svc.SaveBulkMarksAsync(req);

        return ok ? Ok() : BadRequest(new { error = "Failed to save bulk marks" });
    }
    [HttpGet]
    public async Task<IActionResult> GetMyGpa()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        var uid = SessionHelper.GetUid(HttpContext.Session);

        var marks = await _svc.GetStudentMarksAsync(uid);
        var gpas = _svc.GetSubjectGPAs(marks);

        var overall = gpas.Count == 0 ? 0 : gpas.Average(g => g.AverageGPA);

        return Ok(new { gpa = Math.Round(overall, 2) });
    }

    // Student gets their marks
    [HttpGet]
    public async Task<IActionResult> GetMyMarks()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        var uid = SessionHelper.GetUid(HttpContext.Session);
        var list = await _svc.GetStudentMarksAsync(uid);

        return Ok(list);
    }
}