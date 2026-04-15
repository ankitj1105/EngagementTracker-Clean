using EngagementTracker.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Firebase Admin SDK init
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("firebase-credentials.json")
});

// Firestore
Environment.SetEnvironmentVariable(
    "GOOGLE_APPLICATION_CREDENTIALS", "firebase-credentials.json");
var firestoreDb = FirestoreDb.Create("studentengagementtracker");
builder.Services.AddSingleton(firestoreDb);

// Session (for storing logged-in user info)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<EngagementTracker.Services.FirebaseAuthService>();
builder.Services.AddScoped<EngagementTracker.Services.AttendanceService>();
builder.Services.AddScoped<EngagementTracker.Services.MarksService>();
builder.Services.AddScoped<EngagementTracker.Services.AssignmentService>();
builder.Services.AddScoped<EngagementTracker.Services.EngagementService>();
builder.Services.AddScoped<EngagementTracker.Services.FaceAttendanceService>();
builder.Services.AddSession();
builder.Services.AddSingleton<SubjectService>();
builder.Services.AddSignalR();
// ADD THIS LINE alongside your other service registrations
builder.Services.AddScoped<EngagementTracker.Services.LiveClassService>();
builder.Services.AddScoped<EngagementTracker.Services.TimetableService>();

var app = builder.Build();
app.UseExceptionHandler(a => a.Run(async context => {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("{\"error\":\"Internal server error\"}");
    }));
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");
app.MapHub<EngagementTracker.Hubs.QuizHub>("/quizHub");
app.Run();