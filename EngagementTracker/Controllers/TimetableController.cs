using EngagementTracker.Helpers;
using EngagementTracker.Models;
using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

[Route("[controller]")]
public class TimetableController : Controller
{
    private readonly TimetableService _svc;

    public TimetableController(TimetableService svc)
    {
        _svc = svc;
    }

    [HttpGet("Get")]
    public async Task<IActionResult> Get([FromQuery] string section)
    {
        if (string.IsNullOrEmpty(section))
            return BadRequest(new { error = "Section is required" });

        var list = await _svc.GetTimetableAsync(section);
        return Ok(list);
    }

    [HttpPost("Save")]
    public async Task<IActionResult> Save([FromQuery] string section, [FromBody] List<SectionTimetable> entries)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        if (SessionHelper.GetRole(HttpContext.Session) != "teacher")
            return Forbid();

        if (string.IsNullOrEmpty(section))
            return BadRequest(new { error = "Section is required" });

        var ok = await _svc.SaveTimetableAsync(section, entries);
        return ok ? Ok() : BadRequest(new { error = "Failed to save timetable" });
    }
}
