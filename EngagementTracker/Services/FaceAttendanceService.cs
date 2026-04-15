using Emgu.CV.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EngagementTracker.Models;
using Google.Cloud.Firestore;
using System.Drawing;

namespace EngagementTracker.Services;
using EngagementTracker.Models;
public class FaceAttendanceService
{
    private readonly FirestoreDb _db;
    private readonly AttendanceService _attSvc;
    private static CascadeClassifier? _classifier;
    private static readonly object Lock = new();
    public static Dictionary<string, LiveAttendanceModel> _live = new();
    private static LiveClassModel _class = new LiveClassModel();
    public FaceAttendanceService(FirestoreDb db, AttendanceService attSvc)
    {
        _db = db;
        _attSvc = attSvc;
        InitClassifier();
    }

    private static void InitClassifier()
    {
        lock (Lock)
        {
            if (_classifier != null) return;

            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "haarcascade_frontalface_default.xml"
            );

            if (File.Exists(path))
                _classifier = new CascadeClassifier(path);
        }
    }
    public void StartClass(string subject)
    {
        _class = new LiveClassModel
        {
            Subject = subject,
            IsActive = true,
            StartTime = DateTime.Now
        };
    }

    public void EndClass()
    {
        _class.IsActive = false;
    }

    public LiveClassModel GetClass()
    {
        return _class;
    }

    // 🔥 FACE COMPARISON (Histogram based)
    public double CompareFaces(string base64Frame, string base64Stored)
    {
        try
        {
            var img1 = LoadGrayImage(base64Frame);
            var img2 = LoadGrayImage(base64Stored);

            if (img1 == null || img2 == null) return 0;

            var size = new Size(100, 100);
            CvInvoke.Resize(img1, img1, size);
            CvInvoke.Resize(img2, img2, size);

            using var hist1 = new Mat();
            using var hist2 = new Mat();

            CvInvoke.CalcHist(new VectorOfMat(img1), new int[] { 0 },
                null, hist1, new int[] { 256 }, new float[] { 0f, 256f }, false);

            CvInvoke.CalcHist(new VectorOfMat(img2), new int[] { 0 },
                null, hist2, new int[] { 256 }, new float[] { 0f, 256f }, false);

            CvInvoke.Normalize(hist1, hist1, 0, 1, NormType.MinMax);
            CvInvoke.Normalize(hist2, hist2, 0, 1, NormType.MinMax);

            var correlation = CvInvoke.CompareHist(hist1, hist2, HistogramCompMethod.Correl);

            img1.Dispose();
            img2.Dispose();

            return Math.Max(0, correlation * 100);
        }
        catch
        {
            return 0;
        }
    }
    public async Task SaveAttendance(string studentUid, string subjectId, string status)
    {
        var doc = new Dictionary<string, object>
        {
            { "studentUid", studentUid },
            { "subjectId", subjectId },
            { "status", status },
            { "date", DateTime.UtcNow },
            { "markedBy", "faceAI" }
        };

        await _db.Collection("attendance").AddAsync(doc);
    }
    // 🔥 LOAD + FACE CROP
    private Mat? LoadGrayImage(string base64)
    {
        try
        {
            var data = Convert.FromBase64String(
                base64.Contains(",") ? base64.Split(',')[1] : base64
            );

            var tempFile = Path.Combine(Path.GetTempPath(), $"face_{Guid.NewGuid()}.jpg");
            File.WriteAllBytes(tempFile, data);

            var img = new Image<Gray, byte>(tempFile);
            File.Delete(tempFile);

            if (_classifier == null)
                return img.Mat;

            var faces = _classifier.DetectMultiScale(img, 1.1, 4, new Size(40, 40));

            if (faces.Length > 0)
            {
                var face = faces[0]; // first detected face
                var cropped = new Mat(img.Mat, face);
                img.Dispose();
                return cropped;
            }

            return img.Mat;
        }
        catch
        {
            return null;
        }
    }

    // 🔥 GET ALL STUDENTS WITH PHOTOS
    public async Task<List<UserModel>> GetAllStudentFacesAsync()
    {
        var snap = await _db.Collection("users")
            .WhereEqualTo("role", "student")
            .GetSnapshotAsync();

        return snap.Documents.Select(d => new UserModel
        {
            Uid = d.Id,
            Name = d.GetValue<string>("name"),
            RollNo = d.GetValue<string>("rollNo"),
            PhotoData = d.ContainsField("photoData")
                ? d.GetValue<string>("photoData")
                : ""
        })
        .Where(s => !string.IsNullOrEmpty(s.PhotoData))
        .ToList();
    }

    // 🔥 FINAL AUTO ATTENDANCE METHOD
    public async Task<List<FaceAttendanceResult>> MarkAttendanceByFaceAsync(
        string webcamFrame,
        string subjectId,
        string date)
    {
        var students = await GetAllStudentFacesAsync();
        var results = new List<FaceAttendanceResult>();

        foreach (var student in students)
        {
            var similarity = CompareFaces(webcamFrame, student.PhotoData);

            // ✅ STRICT MATCH (important)
            if (similarity >= 60)
            {
                await _attSvc.MarkAttendanceAsync(
                    student.Uid,
                    subjectId,
                    date,
                    "Present",
                    "face-system"
                );

                results.Add(new FaceAttendanceResult
                {
                    StudentUid = student.Uid,
                    StudentName = student.Name,
                    RollNo = student.RollNo,
                    Similarity = Math.Round(similarity, 1),
                    Status = "Present"
                });
                if (!_live.ContainsKey(student.Uid))
                {
                    _live[student.Uid] = new LiveAttendanceModel
                    {
                        StudentUid = student.Uid
                    };
                }

                _live[student.Uid].TotalSeconds += 5;
            }
       
        }

        return results;
    }
}


// 🔥 RESULT MODEL
public class FaceAttendanceResult
{
    public string StudentUid { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string RollNo { get; set; } = "";
    public double Similarity { get; set; }
    public string Status { get; set; } = "";
}