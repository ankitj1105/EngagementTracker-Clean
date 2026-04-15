using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;
using EngagementTracker.Services;

namespace EngagementTracker.Controllers;

public class SubjectController : Controller
{
    private readonly SubjectService _service;

    public SubjectController(SubjectService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subjects = await _service.GetSubjectsAsync();
        return Json(subjects);
    }
}