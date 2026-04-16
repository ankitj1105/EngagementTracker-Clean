using EngagementTracker.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 🔥 READ Firebase JSON from ENV VARIABLE
    var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_JSON");

    if (string.IsNullOrEmpty(firebaseJson))
    {
        throw new Exception("FIREBASE_JSON environment variable is missing!");
    }

    // 🔥 FIX NEWLINE ISSUE (VERY IMPORTANT)
    firebaseJson = firebaseJson.Replace("\\n", "\n");

    // 🔥 Initialize Firebase
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromJson(firebaseJson)
    });

    // 🔥 Firestore
    var firestoreDb = FirestoreDb.Create("studentengagementtracker");
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

    // 🔥 YOUR SERVICES (KEEP THESE)
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

    // 🔥 ERROR HANDLER
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