using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers
{
    // FIX 1: Removed [ApiController] — that attribute prevents View() from working.
    //         A controller that serves both Views AND JSON must be plain Controller.
    // FIX 2: Removed [Route("Teacher")] class-level attribute and replaced with
    //         explicit [Route] on each action so the Dashboard view route works
    //         correctly alongside the API endpoints.
    public class TeacherController : Controller
    {
        private readonly Google.Cloud.Firestore.FirestoreDb _db;

        public TeacherController(Google.Cloud.Firestore.FirestoreDb db)
        {
            _db = db;
        }

        // ─── PAGE ACTIONS (return Views) ──────────────────────────────────────

        // FIX 3: Added Dashboard() action — this is what was missing.
        //         Without it, GET /Teacher/Dashboard returned 404.
        [HttpGet]
        public IActionResult Dashboard()
        {
            var name = HttpContext.Session.GetString("name") ?? "Teacher";
            ViewBag.Name = name;
            return View();
        }

        // Optional convenience pages — add more if you create separate views later
        [HttpGet]
        public IActionResult Students()      => RedirectToAction("Dashboard");
        [HttpGet]
        public IActionResult MarkAttendance() => RedirectToAction("Dashboard");
        [HttpGet]
        public IActionResult Marks()          => RedirectToAction("Dashboard");
        [HttpGet]
        public IActionResult AtRisk()         => RedirectToAction("Dashboard");
        [HttpGet]
        public IActionResult Assignments()    => RedirectToAction("Dashboard");
        [HttpGet]
        public IActionResult Engagement()     => RedirectToAction("Dashboard");

        // ─── API ENDPOINTS (return JSON) ──────────────────────────────────────

        [HttpGet("Teacher/GetStudents")]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var snap = await _db.Collection("users").WhereEqualTo("role", "student").GetSnapshotAsync();
                var list = snap.Documents.Select(doc => new {
                    uid = doc.Id,
                    name = doc.ContainsField("name") ? doc.GetValue<string>("name") : "Unknown",
                    rollNo = doc.ContainsField("rollNo") ? doc.GetValue<string>("rollNo") : "-",
                    section = doc.ContainsField("section") ? doc.GetValue<string>("section") : "-",
                    email = doc.ContainsField("email") ? doc.GetValue<string>("email") : ""
                }).ToList();
                return Ok(list);
            }
            catch { return Ok(new object[] { }); }
        }

        [HttpGet("Teacher/GetAllAttendance")]
        public async Task<IActionResult> GetAllAttendance()
        {
            try
            {
                var snap = await _db.Collection("attendance").GetSnapshotAsync();
                var list = snap.Documents.Select(d => new {
                    studentUid = d.GetValue<string>("studentUid"),
                    subjectId = d.GetValue<string>("subjectId"),
                    date = d.GetValue<string>("date"),
                    status = d.GetValue<string>("status")
                }).ToList();
                return Ok(list);
            }
            catch { return Ok(new object[] { }); }
        }

        [HttpGet("Teacher/GetAllMarks")]
        public async Task<IActionResult> GetAllMarks()
        {
            try
            {
                var snap = await _db.Collection("marks").GetSnapshotAsync();
                var list = snap.Documents.Select(d => new {
                    studentUid = d.ContainsField("studentUid") ? d.GetValue<string>("studentUid") : "",
                    subjectName = d.ContainsField("subjectName") ? d.GetValue<string>("subjectName") : (d.ContainsField("subjectId") ? d.GetValue<string>("subjectId") : "Unknown"),
                    examType = d.ContainsField("examType") ? d.GetValue<string>("examType") : "Quiz",
                    score = d.ContainsField("score") ? Convert.ToDouble(d.GetValue<object>("score")) : 0.0,
                    maxScore = d.ContainsField("maxScore") ? Convert.ToDouble(d.GetValue<object>("maxScore")) : 100.0,
                    grade = d.ContainsField("grade") ? d.GetValue<string>("grade") : "",
                    gpaPoints = d.ContainsField("gpaPoints") ? Convert.ToDouble(d.GetValue<object>("gpaPoints")) : 0.0
                }).ToList();
                return Ok(list);
            }
            catch { return Ok(new object[] { }); }
        }
    }
}
