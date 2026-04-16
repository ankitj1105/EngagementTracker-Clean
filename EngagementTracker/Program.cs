using EngagementTracker.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 🔥 HANDLE FIREBASE CONFIG (LOCAL + AZURE)
    string firebaseJson;

    // 1️⃣ Try ENV (Azure)
    firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_JSON");

    // 2️⃣ If not found → use local file
    if (string.IsNullOrEmpty(firebaseJson))
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "firebase-credentials.json");

        if (!File.Exists(path))
            throw new Exception("Firebase credentials file not found!");

        firebaseJson = File.ReadAllText(path);
    }

    // 🔥 Fix newline issue (important for Azure ENV)
    firebaseJson = firebaseJson.Replace("\\n", "\n");

    // 🔥 Create credential ONCE
    var credential = GoogleCredential.FromJson(firebaseJson);

    // 🔥 Initialize Firebase (safe)
    if (FirebaseApp.DefaultInstance == null)
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential
        });
    }

    // 🔥 FIXED Firestore initialization (NO MORE ADC ERROR)
    var firestoreDb = new FirestoreDbBuilder
    {
        ProjectId = "studentengagementtracker",
        Credential = credential
    }.Build();

    builder.Services.AddSingleton(firestoreDb);

    // 🔥 Session
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddControllersWithViews();

    // 🔥 Your Services
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

    // 🔥 Error handler
    app.UseExceptionHandler(a => a.Run(async context =>
    {
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
}
catch (Exception ex)
{
    // 🔥 WRITE ERROR TO FILE (CRITICAL FOR DEBUGGING)
    try
    {
        File.WriteAllText("startup-error.txt", ex.ToString());
    }
    catch { }

    throw;
}
