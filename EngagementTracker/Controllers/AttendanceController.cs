using EngagementTracker.Helpers;
using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

public class AttendanceController : Controller
{
    private readonly AttendanceService _svc;
    public AttendanceController(AttendanceService svc) { _svc = svc; }

    // Student gets their own attendance
    [HttpGet]
    public async Task<IActionResult> GetMyAttendance()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();
        var uid  = SessionHelper.GetUid(HttpContext.Session);
        var data = await _svc.GetStudentAttendanceAsync(uid);
        return Ok(data);
    }
    [HttpGet]
    public async Task<IActionResult> GetMySummary(string subjectId)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        var uid = SessionHelper.GetUid(HttpContext.Session);

        var summary = await _svc.GetSummaryAsync(uid, subjectId);

        return Ok(summary);
    }

    // Teacher marks attendance
    [HttpPost]
    public async Task<IActionResult> Mark([FromBody] MarkRequest req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();
        var teacherUid = SessionHelper.GetUid(HttpContext.Session);
        var ok = await _svc.MarkAttendanceAsync(
            req.StudentUid, req.SubjectId,
            req.Date, req.Status, teacherUid);
        return ok ? Ok() : BadRequest();
    }
}


public class MarkRequest
{
    public string StudentUid { get; set; } = "";
    public string SubjectId  { get; set; } = "";
    public string Date       { get; set; } = "";
    public string Status     { get; set; } = "";
}