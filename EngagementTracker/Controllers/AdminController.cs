using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EngagementTracker.Controllers
{
    public class AdminController : Controller
    {
        private readonly FirestoreDb _db;

        public AdminController(FirestoreDb db)
        {
            _db = db;
        }

        // ─── PAGE ACTIONS ──────────────────────────────────────────────

        [HttpGet("Admin/Dashboard")]
        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "admin")
            {
                return RedirectToAction("Login", "Auth");
            }
            
            var name = HttpContext.Session.GetString("name") ?? "Admin";
            ViewBag.Name = name;
            return View();
        }

        // ─── API ENDPOINTS ─────────────────────────────────────────────

        [HttpGet("Admin/GetAllStudents")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();

                var snap = await _db.Collection("users").WhereEqualTo("role", "student").GetSnapshotAsync();
                var list = snap.Documents.Select(doc => new {
                    uid = doc.Id,
                    name = doc.ContainsField("name") ? doc.GetValue<string>("name") : "Unknown",
                    rollNo = doc.ContainsField("rollNo") ? doc.GetValue<string>("rollNo") : "-",
                    section = doc.ContainsField("section") ? doc.GetValue<string>("section") : "-",
                    department = doc.ContainsField("department") ? doc.GetValue<string>("department") : "-",
                    email = doc.ContainsField("email") ? doc.GetValue<string>("email") : ""
                }).ToList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching students: {ex.Message}");
                return Ok(new object[] { });
            }
        }

        [HttpGet("Admin/GetAllTeachers")]
        public async Task<IActionResult> GetAllTeachers()
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();

                var snap = await _db.Collection("users").WhereEqualTo("role", "teacher").GetSnapshotAsync();
                var list = snap.Documents.Select(doc => new {
                    uid = doc.Id,
                    name = doc.ContainsField("name") ? doc.GetValue<string>("name") : "Unknown",
                    department = doc.ContainsField("department") ? doc.GetValue<string>("department") : "-",
                    email = doc.ContainsField("email") ? doc.GetValue<string>("email") : ""
                }).ToList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching teachers: {ex.Message}");
                return Ok(new object[] { });
            }
        }
        [HttpGet("Admin/GetAllTimetables")]
        public async Task<IActionResult> GetAllTimetables()
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();

                var snap = await _db.Collection("timetables").GetSnapshotAsync();
                var list = snap.Documents.Select(doc => new {
                    id = doc.Id,
                    section = doc.ContainsField("section") ? doc.GetValue<string>("section") : "-",
                    dayOfWeek = doc.ContainsField("dayOfWeek") ? doc.GetValue<string>("dayOfWeek") : "-",
                    startTime = doc.ContainsField("startTime") ? doc.GetValue<string>("startTime") : "-",
                    endTime = doc.ContainsField("endTime") ? doc.GetValue<string>("endTime") : "-",
                    subject = doc.ContainsField("subject") ? doc.GetValue<string>("subject") : "-",
                    teacherName = doc.ContainsField("teacherName") ? doc.GetValue<string>("teacherName") : "-",
                    room = doc.ContainsField("room") ? doc.GetValue<string>("room") : "-"
                }).ToList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching timetables: {ex.Message}");
                return Ok(new object[] { });
            }
        }

        [HttpGet("Admin/GetAllAttendance")]
        public async Task<IActionResult> GetAllAttendance()
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();

                var snap = await _db.Collection("attendance").GetSnapshotAsync();
                var list = snap.Documents.Select(doc => new {
                    id = doc.Id,
                    studentUid = doc.ContainsField("studentUid") ? doc.GetValue<string>("studentUid") : "-",
                    subjectId = doc.ContainsField("subjectId") ? doc.GetValue<string>("subjectId") : "-",
                    date = doc.ContainsField("date") ? doc.GetValue<string>("date") : "-",
                    status = doc.ContainsField("status") ? doc.GetValue<string>("status") : "-",
                    teacherUid = doc.ContainsField("teacherUid") ? doc.GetValue<string>("teacherUid") : "-"
                }).ToList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching attendance: {ex.Message}");
                return Ok(new object[] { });
            }
        }

        [HttpGet("Admin/GetAllMarks")]
        public async Task<IActionResult> GetAllMarks()
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();

                var snap = await _db.Collection("marks").GetSnapshotAsync();
                var list = snap.Documents.Select(doc => new {
                    id = doc.Id,
                    studentUid = doc.ContainsField("studentUid") ? doc.GetValue<string>("studentUid") : "-",
                    subjectName = doc.ContainsField("subjectName") ? doc.GetValue<string>("subjectName") : (doc.ContainsField("subjectId") ? doc.GetValue<string>("subjectId") : "Unknown"),
                    examType = doc.ContainsField("examType") ? doc.GetValue<string>("examType") : "Quiz",
                    score = doc.ContainsField("score") ? Convert.ToDouble(doc.GetValue<object>("score")) : 0.0,
                    maxScore = doc.ContainsField("maxScore") ? Convert.ToDouble(doc.GetValue<object>("maxScore")) : 100.0,
                    grade = doc.ContainsField("grade") ? doc.GetValue<string>("grade") : "-",
                    gpaPoints = doc.ContainsField("gpaPoints") ? Convert.ToDouble(doc.GetValue<object>("gpaPoints")) : 0.0
                }).ToList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching marks: {ex.Message}");
                return Ok(new object[] { });
            }
        }
        [HttpPost("Admin/DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest req)
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();
                
                if (string.IsNullOrEmpty(req.Uid))
                    return BadRequest(new { error = "User ID is required" });

                await _db.Collection("users").Document(req.Uid).DeleteAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return StatusCode(500, new { error = "Failed to delete user." });
            }
        }
    }

    public class DeleteUserRequest
    {
        public string Uid { get; set; } = "";
    }
}
