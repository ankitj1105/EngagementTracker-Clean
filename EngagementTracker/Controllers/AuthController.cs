using EngagementTracker.Helpers;
using Google.Cloud.Firestore;
using EngagementTracker.Models;
using EngagementTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EngagementTracker.Controllers;

public class AuthController : Controller
{
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreDb _db;

    public AuthController(FirebaseAuthService authService, FirestoreDb db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public IActionResult Login(LoginViewModel model) => View(model);
    
    [HttpPost]
    public async Task<IActionResult> LoginUser([FromBody] LoginViewModel model)
    {
        var snap = await _db.Collection("users")
            .WhereEqualTo("email", model.Email)
            .WhereEqualTo("password", model.Password)
            .GetSnapshotAsync();

        if (snap.Count == 0)
            return Unauthorized(new { error = "Invalid credentials" });

        var doc = snap.Documents[0];

        var uid = doc.Id;
        var name = doc.GetValue<string>("name");
        var role = doc.GetValue<string>("role");
        var section = doc.ContainsField("section") ? doc.GetValue<string>("section") : "";

        HttpContext.Session.SetString("uid", uid);
        HttpContext.Session.SetString("name", name);
        HttpContext.Session.SetString("role", role);
        HttpContext.Session.SetString("section", section);

        return Ok(new { role });
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> RegisterProfile([FromBody] RegisterViewModel model)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { "name",         model.Name         },
                { "email",        model.Email        },
                { "role",         model.Role         },
                { "section",      model.Section      },
                { "rollNo",       model.RollNo       },
                { "department",   model.Department   },
                { "enrollmentNo", model.EnrollmentNo },
                { "createdAt",    DateTime.UtcNow.ToString("yyyy-MM-dd") }
            };

            // Save photo for students
            if (model.Role == "student" && !string.IsNullOrEmpty(model.PhotoData))
                data["photoData"] = model.PhotoData;

            await _db.Collection("users").Document(model.Uid).SetAsync(data);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpPost]
    public async Task<IActionResult> SavePhoto([FromBody] PhotoModel model)
    {
        try
        {
            var uid = HttpContext.Session.GetString("uid");

            if (string.IsNullOrEmpty(uid))
                return Unauthorized();

            var userRef = _db.Collection("users").Document(uid);

            await userRef.UpdateAsync(new Dictionary<string, object>
            {
                { "photoData", model.PhotoData }
            });

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest();
        }
    }
    [HttpPost]
    public async Task<IActionResult> VerifyToken([FromBody] TokenRequest req)
    {
        try
        {
            var decoded = await FirebaseAdmin.Auth.FirebaseAuth
                .DefaultInstance.VerifyIdTokenAsync(req.IdToken);

            var uid = decoded.Uid;
            var (ok, name, role) = await _authService.GetUserProfileAsync(uid);

            if (!ok)
                return Unauthorized((object)new { error = "Profile not found" });

            HttpContext.Session.SetString("uid", uid);
            HttpContext.Session.SetString("name", name);
            HttpContext.Session.SetString("role", role);

            return Ok(new { role });
        }
        catch (Exception ex)
        {
            return Unauthorized((object)new { error = ex.Message });
        }
    }
    [HttpGet]
  
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized();

        var uid = SessionHelper.GetUid(HttpContext.Session);

        var doc = await _db.Collection("users").Document(uid).GetSnapshotAsync();

        if (!doc.Exists)
            return NotFound();

        return Ok(new
        {
            name = doc.GetValue<string>("name"),
            role = doc.GetValue<string>("role"),
            section = doc.ContainsField("section") ? doc.GetValue<string>("section") : "",
            department = doc.ContainsField("department") ? doc.GetValue<string>("department") : "",
            rollNo = doc.ContainsField("rollNo") ? doc.GetValue<string>("rollNo") : "",
            photoData = doc.ContainsField("photoData") 
                ? doc.GetValue<string>("photoData") 
                : ""
        });
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}

public class RegisterProfileRequest
{
    public string Uid     { get; set; } = "";
    public string Name    { get; set; } = "";
    public string Email   { get; set; } = "";
    public string Role    { get; set; } = "";
    public string Section { get; set; } = "";
    public string RollNo  { get; set; } = "";
}

public class TokenRequest
{
    public string IdToken { get; set; } = "";
}