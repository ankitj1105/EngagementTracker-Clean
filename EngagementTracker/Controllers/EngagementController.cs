using EngagementTracker.Helpers;
using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

public class EngagementController : Controller
{
    private readonly EngagementService _svc;
    public EngagementController(EngagementService svc) { _svc = svc; }

    // Student sends a webcam frame for analysis
    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] FrameRequest req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        var uid  = SessionHelper.GetUid(HttpContext.Session);
        var name = SessionHelper.GetName(HttpContext.Session);

        var faceDetected = _svc.DetectFace(req.Frame);
        var score        = req.CurrentScore + (faceDetected ? 5 : -3);
        score            = Math.Clamp(score, 0, 100);

        await _svc.SaveEngagementAsync(uid, name, req.SessionId, score, faceDetected);

        return Ok(new { score, faceDetected });
    }

    // Teacher gets all student summaries
    [HttpGet]
    public async Task<IActionResult> GetSummaries()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();
        var list = await _svc.GetAllSummariesAsync();
        return Ok(list);
    }

    // Student gets own logs (old method)
    [HttpGet]
    public async Task<IActionResult> GetMyLogs()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();
        var uid  = SessionHelper.GetUid(HttpContext.Session);
        var list = await _svc.GetMyLogsAsync(uid);
        return Ok(list);
    }

    // Get grouped sessions
    [HttpGet]
    public async Task<IActionResult> GetStudentSessions([FromQuery] string? studentUid)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();
            
        var uid = string.IsNullOrWhiteSpace(studentUid) ? SessionHelper.GetUid(HttpContext.Session) : studentUid;
        if (uid == null) return Unauthorized();
        
        var list = await _svc.GetStudentSessionsAsync(uid);
        return Ok(list);
    }

    // Get specific session plot data
    [HttpGet]
    public async Task<IActionResult> GetSessionData([FromQuery] string sessionId, [FromQuery] string? studentUid)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();
            
        var uid = string.IsNullOrWhiteSpace(studentUid) ? SessionHelper.GetUid(HttpContext.Session) : studentUid;
        if (uid == null) return Unauthorized();
            
        var list = await _svc.GetSessionDataAsync(sessionId, uid);
        return Ok(list);
    }
}

public class FrameRequest
{
    public string Frame        { get; set; } = "";
    public string SessionId    { get; set; } = "";
    public int    CurrentScore { get; set; } = 50;
}