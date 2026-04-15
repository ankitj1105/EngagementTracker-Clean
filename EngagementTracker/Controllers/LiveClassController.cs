using Microsoft.AspNetCore.Mvc;
using EngagementTracker.Helpers;
using EngagementTracker.Services;
using EngagementTracker.Models;

namespace EngagementTracker.Controllers
{
    public class LiveClassController : Controller
    {
        private readonly LiveClassService _liveClassService;

        public LiveClassController(LiveClassService liveClassService)
        {
            _liveClassService = liveClassService;
        }

        // ── STUDENT: Opens the live class page ───────────────────────────
        public async Task<IActionResult> Index()
        {
            try
            {
                if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                    return RedirectToAction("Login", "Auth");

                var session = await _liveClassService.GetActiveSessionAsync();
                var uid = SessionHelper.GetUid(HttpContext.Session);
                var role = SessionHelper.GetRole(HttpContext.Session);

                ViewBag.Role = role;
                var vm = new LiveClassViewModel { ActiveSession = session };

                // Only students have attendance logs in liveStudentLogs.
                if (session != null && role == "student" && !string.IsNullOrWhiteSpace(uid))
                {
                    var log = await _liveClassService.GetStudentLogAsync(session.Id, uid);
                    if (log != null)
                    {
                        vm.IsAlreadyMarked = log.AutoMarked;
                        vm.MinutesSpent    = log.MinutesPresent;
                    }
                }

                return View("~/Views/Student/LiveClass.cshtml", vm);
            }
            catch
            {
                return RedirectToAction("Dashboard", SessionHelper.GetRole(HttpContext.Session) == "teacher" ? "Teacher" : "Student");
            }
        }

        // ── TEACHER: Start a class ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> StartClass([FromBody] StartClassRequest? req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Subject))
                return BadRequest(new { success = false, message = "Subject is required" });

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            var uid = SessionHelper.GetUid(HttpContext.Session);
            if (uid == null) return Unauthorized();

            var sessionId = await _liveClassService.StartSessionAsync(
                uid, req.Subject, req.DurationMinutes);

            return Json(new { success = true, sessionId });
        }

        // ── TEACHER: End the class ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> EndClass([FromBody] EndClassRequest? req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SessionId))
                return BadRequest(new { success = false, message = "Session ID is required" });

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            await _liveClassService.EndSessionAsync(req.SessionId);
            return Json(new { success = true });
        }

        // ── STUDENT: Join / record entry time ───────────────────────────
        [HttpPost]
        public async Task<IActionResult> JoinClass([FromBody] JoinClassRequest? req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SessionId))
                return BadRequest(new { success = false, message = "Session ID is required" });

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            var uid  = SessionHelper.GetUid(HttpContext.Session);
            var name = SessionHelper.GetName(HttpContext.Session);
            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(name))
                return Unauthorized();

            try
            {
                var logId = await _liveClassService.StudentJoinAsync(req.SessionId, uid, name);
                return Json(new { success = true, logId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Unable to join class right now." });
            }
        }

        // ── STUDENT: Heartbeat (called every 30 seconds from browser) ───
        [HttpPost]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest? req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SessionId))
                return BadRequest(new { success = false, message = "Session ID is required" });

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            var uid = SessionHelper.GetUid(HttpContext.Session);
            if (uid == null) return Unauthorized();

            var justMarked = await _liveClassService.UpdatePresenceAsync(req.SessionId, uid);
            return Json(new { justMarked });
        }

        // ── BOTH: Get active session info (polling) ──────────────────────
        [HttpGet]
        public async Task<IActionResult> Status()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            var session = await _liveClassService.GetActiveSessionAsync();
            if (session == null)
                return Json(new { isLive = false });

            // Jitsi Room Name Generation (Same as View)
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var safeSubject = string.IsNullOrWhiteSpace(session.Subject) ? "Live-Class" : session.Subject;
            var roomName = "GLA-" + safeSubject.Replace(" ", "-") + "-" + today;
            var link = $"https://meet.jit.si/{roomName}";

            return Json(new
            {
                isLive = true,
                sessionId = session.Id,
                subject = session.Subject,
                link = link
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLink()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            var session = await _liveClassService.GetActiveSessionAsync();
            if (session == null)
                return Json(new { error = "No live class is running right now." });

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveSession()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Unauthorized();

            var session = await _liveClassService.GetActiveSessionAsync();
            if (session == null)
                return Json(new { isLive = false });

            return Json(new
            {
                isLive          = true,
                sessionId       = session.Id,
                subject         = session.Subject,
                startTime       = session.StartTime.ToString("o"),
                durationMinutes = session.DurationMinutes,
                thresholdPercent = session.AttendanceThresholdPercent
            });
        }
    }

    // ── Request models ───────────────────────────────────────────────────
    public class StartClassRequest
    {
        public string Subject { get; set; } = "";
        public int DurationMinutes { get; set; } = 60;
    }
    public class EndClassRequest   { public string SessionId { get; set; } = ""; }
    public class JoinClassRequest  { public string SessionId { get; set; } = ""; }
    public class HeartbeatRequest  { public string SessionId { get; set; } = ""; }
}