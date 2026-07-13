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
        [HttpPost("Admin/UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest req)
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();
                
                if (string.IsNullOrEmpty(req.Uid))
                    return BadRequest(new { error = "User ID is required" });

                var updateData = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(req.Name)) updateData["name"] = req.Name;
                if (!string.IsNullOrEmpty(req.Email)) updateData["email"] = req.Email;
                if (!string.IsNullOrEmpty(req.Department)) updateData["department"] = req.Department;
                
                // Allow empty strings for section / rollNo if they are clearing it or it's a teacher,
                // but usually we check if the request provides non-null to update
                if (req.Section != null) updateData["section"] = req.Section;
                if (req.RollNo != null) updateData["rollNo"] = req.RollNo;

                if (updateData.Count > 0)
                {
                    await _db.Collection("users").Document(req.Uid).UpdateAsync(updateData);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return StatusCode(500, new { error = "Failed to update user." });
            }
        }
        [HttpPost("Admin/AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest req)
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "admin") return Unauthorized();

                if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password) || string.IsNullOrEmpty(req.Role))
                    return BadRequest(new { error = "Email, Password, and Role are required." });

                var existingUser = await _db.Collection("users").WhereEqualTo("email", req.Email).GetSnapshotAsync();
                if (existingUser.Count > 0)
                    return BadRequest(new { error = "Email is already in use." });

                var uid = Guid.NewGuid().ToString();
                var data = new Dictionary<string, object>
                {
                    { "name", req.Name ?? "" },
                    { "email", req.Email },
                    { "password", req.Password },
                    { "role", req.Role },
                    { "department", req.Department ?? "" },
                    { "createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd") }
                };

                if (req.Role == "student")
                {
                    data["section"] = req.Section ?? "";
                    data["rollNo"] = req.RollNo ?? "";
                }

                await _db.Collection("users").Document(uid).SetAsync(data);
                return Ok(new { success = true, uid });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user: {ex.Message}");
                return StatusCode(500, new { error = "Failed to add user." });
            }
        }
    }

    public class DeleteUserRequest
    {
        public string Uid { get; set; } = "";
    }

    public class UpdateUserRequest
    {
        public string Uid { get; set; } = "";
        public string Name { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string RollNo { get; set; }
    }
    public class AddUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string RollNo { get; set; }
    }
}
