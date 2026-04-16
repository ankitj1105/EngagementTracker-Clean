using EngagementTracker.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);


// 🔥 STEP 1: Read Firebase JSON from Azure Environment Variable
var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_JSON");

if (string.IsNullOrEmpty(firebaseJson))
{
    throw new Exception("FIREBASE_JSON environment variable not found");
}


// 🔥 STEP 2: Initialize Firebase using JSON (NOT file)
var credential = GoogleCredential.FromJson(firebaseJson);

FirebaseApp.Create(new AppOptions()
{
    Credential = credential
});


// 🔥 STEP 3: Firestore (NO file path needed anymore)
var firestoreDb = FirestoreDb.Create("studentengagementtracker");
builder.Services.AddSingleton(firestoreDb);


// ✅ Session (for storing logged-in user info)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// ✅ MVC + Services
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<MarksService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<EngagementService>();
builder.Services.AddScoped<FaceAttendanceService>();
builder.Services.AddScoped<LiveClassService>();
builder.Services.AddScoped<TimetableService>();

builder.Services.AddSingleton<SubjectService>();
builder.Services.AddSignalR();


var app = builder.Build();


// ✅ Global error handler
app.UseExceptionHandler(a => a.Run(async context =>
{
    context.Response.ContentType = "application/json";
    context.Response.StatusCode = 500;
    await context.Response.WriteAsync("{\"error\":\"Internal server error\"}");
}));


// ✅ Middleware pipeline
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();


// ✅ Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapHub<EngagementTracker.Hubs.QuizHub>("/quizHub");

app.Run();