using EngagementTracker.Helpers;
using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

public class FaceAttendanceController : Controller
{
    private readonly FaceAttendanceService _svc;
    public FaceAttendanceController(FaceAttendanceService svc) { _svc = svc; }

    // Teacher triggers face scan
    [HttpPost]
    public async Task<IActionResult> Scan([FromBody] FaceScanRequest req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        if (SessionHelper.GetRole(HttpContext.Session) != "teacher") return Forbid();

        var results = await _svc.MarkAttendanceByFaceAsync(
            req.Frame, req.SubjectId,
            req.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd"));

        return Ok(results);
    }
    [HttpPost]
    public IActionResult StartClass([FromBody] string subject)
    {
        _svc.StartClass(subject);
        return Ok();
    }

    [HttpPost]
    public IActionResult EndClass()
    {
        _svc.EndClass();
        return Ok();
    }

    [HttpGet]
    public IActionResult GetLiveClass()
    {
        return Ok(_svc.GetClass());
    }
    [HttpPost]
    public async Task<IActionResult> EndSession([FromQuery] string subjectId)
    {
        var presentThreshold = 210; // 3.5 minutes

        var results = new List<object>();

        foreach (var s in FaceAttendanceService._live.Values)
        {
            var status = s.TotalSeconds >= presentThreshold ? "Present" : "Absent";

            await _svc.SaveAttendance(s.StudentUid, subjectId, status);

            results.Add(new
            {
                student = s.StudentUid,
                time = s.TotalSeconds,
                status = status
            });
        }

        FaceAttendanceService._live.Clear();

        return Ok(results);
    }
    // Get student's stored photo (for verification preview)
    [HttpGet]
    public async Task<IActionResult> GetStudentPhotos()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session)) return Unauthorized();
        var students = await _svc.GetAllStudentFacesAsync();
        return Ok(students.Select(s => new { s.Uid, s.Name, s.RollNo }));
    }
}

public class FaceScanRequest
{
    public string Frame     { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string? Date     { get; set; }
}