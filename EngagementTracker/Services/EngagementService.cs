using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EngagementTracker.Models;
using Google.Cloud.Firestore;

namespace EngagementTracker.Services;

public class EngagementService
{
    private readonly FirestoreDb _db;
    private static CascadeClassifier? _faceClassifier;
    private static readonly object Lock = new();

    public EngagementService(FirestoreDb db)
    {
        _db = db;
        InitClassifier();
    }

    private static void InitClassifier()
    {
        lock (Lock)
        {
            if (_faceClassifier != null) return;
            var cascadePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "haarcascade_frontalface_default.xml"
            );
            if (File.Exists(cascadePath))
                _faceClassifier = new CascadeClassifier(cascadePath);
        }
    }

    public bool DetectFace(string base64Image)
    {
        try
        {
            if (_faceClassifier == null) return true;

            var imageData = Convert.FromBase64String(
                base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image
            );

            // Write bytes to a temp file and read with EmguCV
            var tempFile = Path.Combine(Path.GetTempPath(), $"frame_{Guid.NewGuid()}.jpg");
            File.WriteAllBytes(tempFile, imageData);

            using var img = new Image<Bgr, byte>(tempFile);
            using var gray = img.Convert<Gray, byte>();

            File.Delete(tempFile);

            var faces = _faceClassifier.DetectMultiScale(
                gray, 1.1, 4,
                new System.Drawing.Size(60, 60)
            );

            return faces.Length > 0;
        }
        catch
        {
            return true;
        }
    }

    public async Task SaveEngagementAsync(
        string studentUid, string studentName,
        string sessionId, int score, bool faceDetected)
    {
        try
        {
            await _db.Collection("engagementLogs").AddAsync(new Dictionary<string, object>
            {
                { "studentUid",      studentUid   },
                { "studentName",     studentName  },
                { "engagementScore", score        },
                { "sessionId",       sessionId    },
                { "faceDetected",    faceDetected },
                { "timestamp",       DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
            });
        }
        catch { }
    }

    public async Task<List<EngagementSummary>> GetAllSummariesAsync()
    {
        var snap = await _db.Collection("engagementLogs").GetSnapshotAsync();
        return snap.Documents
            .GroupBy(d => d.GetValue<string>("studentUid"))
            .Select(g => new EngagementSummary
            {
                StudentUid    = g.Key,
                StudentName   = g.First().GetValue<string>("studentName"),
                AverageScore  = Math.Round(g.Average(d => (double)d.GetValue<long>("engagementScore")), 1),
                TotalSessions = g.Select(d => d.GetValue<string>("sessionId")).Distinct().Count(),
                LastActive    = g.Max(d => d.GetValue<string>("timestamp")) ?? ""
            }).ToList();
    }

    public async Task<List<EngagementLog>> GetMyLogsAsync(string studentUid)
    {
        var snap = await _db.Collection("engagementLogs")
            .WhereEqualTo("studentUid", studentUid)
            .GetSnapshotAsync();

        return snap.Documents.Select(d => new EngagementLog
        {
            Id              = d.Id,
            StudentUid      = d.GetValue<string>("studentUid"),
            StudentName     = d.GetValue<string>("studentName"),
            EngagementScore = (int)d.GetValue<long>("engagementScore"),
            SessionId       = d.GetValue<string>("sessionId"),
            FaceDetected    = d.GetValue<bool>("faceDetected"),
            Timestamp       = d.GetValue<string>("timestamp")
        }).OrderByDescending(l => l.Timestamp).Take(50).ToList();
    }

    public async Task<List<object>> GetStudentSessionsAsync(string studentUid)
    {
        var snap = await _db.Collection("engagementLogs")
            .WhereEqualTo("studentUid", studentUid)
            .GetSnapshotAsync();

        var sessions = snap.Documents
            .GroupBy(d => d.GetValue<string>("sessionId"))
            .Select(g => new
            {
                SessionId = g.Key,
                StartTimestamp = g.Min(d => d.GetValue<string>("timestamp")) ?? "",
                EndTimestamp = g.Max(d => d.GetValue<string>("timestamp")) ?? "",
                AverageScore = Math.Round(g.Average(d => (double)d.GetValue<long>("engagementScore")), 1),
                Checks = g.Count()
            })
            .OrderByDescending(s => s.StartTimestamp)
            .ToList();

        var result = new List<object>();
        foreach (var s in sessions)
        {
            string className = "Self Study";
            string durationStr = "1 min";
            bool isLiveClass = false;

            if (!string.IsNullOrEmpty(s.SessionId) && !s.SessionId.StartsWith("session_"))
            {
                try {
                    var sessionDoc = await _db.Collection("liveSessions").Document(s.SessionId).GetSnapshotAsync();
                    if (sessionDoc.Exists)
                    {
                        var data = sessionDoc.ToDictionary();
                        className = data.ContainsKey("subject") ? data["subject"]?.ToString() ?? "Live Class" : "Live Class";
                        isLiveClass = true;
                    }
                } catch { }
            }
            
            if (DateTime.TryParse(s.StartTimestamp, out var st) && DateTime.TryParse(s.EndTimestamp, out var et))
            {
                var dur = (et - st).TotalMinutes;
                durationStr = Math.Max(1, Math.Round(dur)) + " min";
            }

            result.Add(new
            {
                sessionId = s.SessionId,
                timestamp = s.StartTimestamp,
                duration = durationStr,
                className = className,
                isLiveClass = isLiveClass,
                averageScore = s.AverageScore,
                checks = s.Checks
            });
        }

        return result;
    }

    public async Task<List<object>> GetSessionDataAsync(string sessionId, string studentUid)
    {
        var snap = await _db.Collection("engagementLogs")
            .WhereEqualTo("sessionId", sessionId)
            .WhereEqualTo("studentUid", studentUid)
            .GetSnapshotAsync();

        var data = snap.Documents
            .Select(d => new
            {
                Timestamp = d.GetValue<string>("timestamp"),
                Score = (int)d.GetValue<long>("engagementScore")
            })
            .OrderBy(d => d.Timestamp)
            .Cast<object>()
            .ToList();

        return data;
    }
}