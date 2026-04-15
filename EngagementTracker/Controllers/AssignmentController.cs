using EngagementTracker.Helpers;
using EngagementTracker.Models;
using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

public class AssignmentController : Controller
{
    private readonly AssignmentService _svc;
    public AssignmentController(AssignmentService svc) { _svc = svc; }

    // Get all assignments (both student and teacher)
   

    // Teacher creates assignment
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AssignmentModel model)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        model.TeacherUid = SessionHelper.GetUid(HttpContext.Session);
        var ok = await _svc.CreateAssignmentAsync(model);
        return ok ? Ok() : BadRequest((object)new { error = "Failed to create" });
    }

    // Student submits
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitRequest req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        var uid  = SessionHelper.GetUid(HttpContext.Session);
        var name = SessionHelper.GetName(HttpContext.Session);
        var sub  = new SubmissionModel
        {
            AssignmentId = req.AssignmentId,
            StudentUid   = uid,
            StudentName  = name,
            FileUrl      = req.FileUrl,
            FileName     = req.FileName
        };
        var ok = await _svc.SubmitAssignmentAsync(sub);
        return ok ? Ok() : BadRequest((object)new { error = "Failed to submit" });
    }

    // Get my submissions (student)
    [HttpGet]
    public async Task<IActionResult> GetMySubmissions()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        var uid  = SessionHelper.GetUid(HttpContext.Session);
        var list = await _svc.GetMySubmissionsAsync(uid);
        return Ok(list);
    }

    // Get submissions for assignment (teacher)
    [HttpGet]
    public async Task<IActionResult> GetSubmissions(string assignmentId)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        var list = await _svc.GetSubmissionsAsync(assignmentId);
        return Ok(list);
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        var list = await _svc.GetAllAssignmentsAsync();
        return Ok(list);
    }

    // Teacher grades submission
    [HttpPost]
    public async Task<IActionResult> Grade([FromBody] GradeRequest req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        var ok = await _svc.GradeSubmissionAsync(req.SubmissionId, req.Grade, req.Feedback);
        return ok ? Ok() : BadRequest((object)new { error = "Failed to grade" });
    }
}

public class SubmitRequest
{
    public string AssignmentId { get; set; } = "";
    public string FileUrl      { get; set; } = "";
    public string FileName     { get; set; } = "";
}

public class GradeRequest
{
    public string SubmissionId { get; set; } = "";
    public double Grade        { get; set; }
    public string Feedback     { get; set; } = "";
}