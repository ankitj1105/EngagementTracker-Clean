using Google.Cloud.Firestore;
using EngagementTracker.Models;

namespace EngagementTracker.Services
{
    public class LiveClassService
    {
        private readonly FirestoreDb _db;
        private readonly AttendanceService _attendanceService;

        public LiveClassService(FirestoreDb db, AttendanceService attendanceService)
        {
            _db = db;
            _attendanceService = attendanceService;
        }

        // ── TEACHER: Start a live class ──────────────────────────────────
        public async Task<string> StartSessionAsync(string teacherUid, string subject, int durationMinutes = 60)
        {
            // End any existing live session for this teacher first
            await EndTeacherActiveSessionAsync(teacherUid);

            var session = new Dictionary<string, object>
            {
                { "teacherUid",               teacherUid },
                { "subject",                  subject },
                { "date",                     DateTime.Now.ToString("yyyy-MM-dd") },
                { "startTime",                Timestamp.FromDateTime(DateTime.UtcNow) },
                { "isLive",                   true },
                { "durationMinutes",          durationMinutes },
                { "attendanceThresholdPercent", 80 }
            };

            var docRef = await _db.Collection("liveSessions").AddAsync(session);
            return docRef.Id;
        }

        // ── TEACHER: End the live class and auto-mark all eligible students ─
        public async Task EndSessionAsync(string sessionId)
        {
            var sessionRef = _db.Collection("liveSessions").Document(sessionId);
            var sessionSnap = await sessionRef.GetSnapshotAsync();
            if (!sessionSnap.Exists) return;

            var endTime = DateTime.UtcNow;
            var startTime = sessionSnap.GetValue<Timestamp>("startTime").ToDateTime();
            var totalMinutes = (endTime - startTime).TotalMinutes;
            var subject = sessionSnap.GetValue<string>("subject");
            var threshold = sessionSnap.GetValue<int>("attendanceThresholdPercent");
            var date = sessionSnap.GetValue<string>("date");

            // Mark session as ended
            await sessionRef.UpdateAsync(new Dictionary<string, object>
            {
                { "isLive",   false },
                { "endTime",  Timestamp.FromDateTime(endTime) }
            });

            // Get all student logs for this session
            var logsSnap = await _db.Collection("liveStudentLogs")
                .WhereEqualTo("sessionId", sessionId)
                .GetSnapshotAsync();

            foreach (var logDoc in logsSnap.Documents)
            {
                var studentUid  = logDoc.GetValue<string>("studentUid");
                var studentName = logDoc.GetValue<string>("studentName");
                var joinTime    = logDoc.GetValue<Timestamp>("joinTime").ToDateTime();

                // If student is still in class (no leaveTime), use endTime
                double minutesPresent;
                try
                {
                    var leaveTime = logDoc.GetValue<Timestamp>("leaveTime").ToDateTime();
                    minutesPresent = (leaveTime - joinTime).TotalMinutes;
                }
                catch
                {
                    minutesPresent = (endTime - joinTime).TotalMinutes;
                    // Update log with leaveTime
                    await _db.Collection("liveStudentLogs").Document(logDoc.Id)
                        .UpdateAsync("leaveTime", Timestamp.FromDateTime(endTime));
                }

                var percentPresent = totalMinutes > 0
                    ? (minutesPresent / totalMinutes) * 100
                    : 0;

                var alreadyMarked = logDoc.GetValue<bool>("autoMarked");

                if (!alreadyMarked && percentPresent >= threshold)
                {
                    await _attendanceService.MarkAttendanceAsync(
                        studentUid, subject, date, "Present", "auto-live");

                    await _db.Collection("liveStudentLogs").Document(logDoc.Id)
                        .UpdateAsync(new Dictionary<string, object>
                        {
                            { "autoMarked",     true },
                            { "minutesPresent", minutesPresent }
                        });
                }
            }
        }

        // ── STUDENT: Record that student joined the live class ────────────
        public async Task<string> StudentJoinAsync(string sessionId, string studentUid, string studentName)
        {
            var sessionRef = _db.Collection("liveSessions").Document(sessionId);
            var sessionSnap = await sessionRef.GetSnapshotAsync();
            if (!sessionSnap.Exists)
                throw new InvalidOperationException("Live class session was not found.");

            var isLive = sessionSnap.ContainsField("isLive") && sessionSnap.GetValue<bool>("isLive");
            if (!isLive)
                throw new InvalidOperationException("This live class has already ended.");

            try
            {
                // Check if already logged in this session
                var existing = await _db.Collection("liveStudentLogs")
                    .WhereEqualTo("sessionId", sessionId)
                    .WhereEqualTo("studentUid", studentUid)
                    .GetSnapshotAsync();

                if (existing.Documents.Count > 0)
                    return existing.Documents[0].Id; // already has a log

                var log = new Dictionary<string, object>
                {
                    { "sessionId",   sessionId },
                    { "studentUid",  studentUid },
                    { "studentName", studentName },
                    { "joinTime",    Timestamp.FromDateTime(DateTime.UtcNow) },
                    { "autoMarked",  false },
                    { "minutesPresent", 0.0 }
                };

                var docRef = await _db.Collection("liveStudentLogs").AddAsync(log);
                return docRef.Id;
            }
            catch
            {
                throw new InvalidOperationException("Could not record class join right now.");
            }
        }

        // ── STUDENT: Called every 30 seconds from browser (heartbeat) ────
        // Returns true if student just crossed the 80% threshold
        public async Task<bool> UpdatePresenceAsync(string sessionId, string studentUid)
        {
            try
            {
                var sessionSnap = await _db.Collection("liveSessions").Document(sessionId).GetSnapshotAsync();
                if (!sessionSnap.Exists) return false;

                var sData      = sessionSnap.ToDictionary();
                var startTime  = sData.ContainsKey("startTime") && sData["startTime"] is Timestamp sts ? sts.ToDateTime() : DateTime.UtcNow;
                var subject    = sData.ContainsKey("subject") ? sData["subject"]?.ToString() ?? "Live Class" : "Live Class";
                var date       = sData.ContainsKey("date") ? sData["date"]?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");
                var threshold  = sData.ContainsKey("attendanceThresholdPercent") ? Convert.ToInt32(sData["attendanceThresholdPercent"]) : 80;
                var durationMins = sData.ContainsKey("durationMinutes") ? Convert.ToInt32(sData["durationMinutes"]) : 60;

                var logSnap = await _db.Collection("liveStudentLogs")
                    .WhereEqualTo("sessionId", sessionId)
                    .WhereEqualTo("studentUid", studentUid)
                    .GetSnapshotAsync();

                if (logSnap.Documents.Count == 0) return false;

                var logDoc     = logSnap.Documents[0];
                var lData      = logDoc.ToDictionary();
                var alreadyMarked = lData.ContainsKey("autoMarked") ? Convert.ToBoolean(lData["autoMarked"]) : false;
                if (alreadyMarked) return false;

                var joinTime       = lData.ContainsKey("joinTime") && lData["joinTime"] is Timestamp lts ? lts.ToDateTime() : DateTime.UtcNow;
                var minutesPresent = (DateTime.UtcNow - joinTime).TotalMinutes;
                var totalMinutes   = Math.Max(durationMins, (DateTime.UtcNow - startTime).TotalMinutes);
                var percent        = (minutesPresent / totalMinutes) * 100;

                // Update running minutes
                await _db.Collection("liveStudentLogs").Document(logDoc.Id)
                    .UpdateAsync("minutesPresent", minutesPresent);

                if (percent >= threshold)
                {
                    // AUTO-MARK PRESENT
                    await _attendanceService.MarkAttendanceAsync(
                        studentUid, subject, date, "Present", "auto-live");

                    await _db.Collection("liveStudentLogs").Document(logDoc.Id)
                        .UpdateAsync("autoMarked", true);

                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        // ── Get the currently active session for any teacher ─────────────
        public async Task<LiveSession?> GetActiveSessionAsync()
        {
            try
            {
                var snap = await _db.Collection("liveSessions")
                    .WhereEqualTo("isLive", true)
                    .GetSnapshotAsync();

                if (snap.Documents.Count == 0) return null;

                foreach (var doc in snap.Documents)
                {
                    var data = doc.ToDictionary();
                    var startTime = data.ContainsKey("startTime") && data["startTime"] is Timestamp ts ? ts.ToDateTime() : DateTime.UtcNow;
                    var durationMinutes = data.ContainsKey("durationMinutes") ? Convert.ToInt32(data["durationMinutes"]) : 60;
                    
                    // If session started more than (duration + 60) minutes ago, it's stale. Close it automatically.
                    if (DateTime.UtcNow > startTime.AddMinutes(durationMinutes + 60))
                    {
                        // Stale, end it in background without blocking
                        _ = EndSessionAsync(doc.Id);
                        continue;
                    }

                    return new LiveSession
                    {
                        Id              = doc.Id,
                        TeacherUid      = data.ContainsKey("teacherUid") ? data["teacherUid"]?.ToString() ?? "" : "",
                        Subject         = data.ContainsKey("subject") ? data["subject"]?.ToString() ?? "Live Class" : "Live Class",
                        Date            = data.ContainsKey("date") ? data["date"]?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd"),
                        StartTime       = startTime,
                        IsLive          = true,
                        DurationMinutes = durationMinutes,
                        AttendanceThresholdPercent = data.ContainsKey("attendanceThresholdPercent") ? Convert.ToInt32(data["attendanceThresholdPercent"]) : 80
                    };
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ── Helper: find and end teacher's last session ───────────────────
        private async Task EndTeacherActiveSessionAsync(string teacherUid)
        {
            try
            {
                var snap = await _db.Collection("liveSessions")
                    .WhereEqualTo("teacherUid", teacherUid)
                    .WhereEqualTo("isLive", true)
                    .GetSnapshotAsync();

                foreach (var doc in snap.Documents)
                    await EndSessionAsync(doc.Id);
            }
            catch { /* Ignore errors during session cleanup */ }
        }

        // ── Get a student's log for a session ─────────────────────────────
        public async Task<StudentSessionLog?> GetStudentLogAsync(string sessionId, string studentUid)
        {
            try
            {
                var snap = await _db.Collection("liveStudentLogs")
                    .WhereEqualTo("sessionId", sessionId)
                    .WhereEqualTo("studentUid", studentUid)
                    .GetSnapshotAsync();

                if (snap.Documents.Count == 0) return null;
                var doc = snap.Documents[0];
                var data = doc.ToDictionary();

                return new StudentSessionLog
                {
                    Id           = doc.Id,
                    SessionId    = sessionId,
                    StudentUid   = studentUid,
                    StudentName  = data.ContainsKey("studentName") ? data["studentName"]?.ToString() ?? "Unknown" : "Unknown",
                    JoinTime     = data.ContainsKey("joinTime") && data["joinTime"] is Timestamp ts ? ts.ToDateTime() : DateTime.UtcNow,
                    MinutesPresent = data.ContainsKey("minutesPresent") ? Convert.ToDouble(data["minutesPresent"]) : 0.0,
                    AutoMarked   = data.ContainsKey("autoMarked") ? Convert.ToBoolean(data["autoMarked"]) : false
                };
            }
            catch
            {
                return null;
            }
        }
    }
}