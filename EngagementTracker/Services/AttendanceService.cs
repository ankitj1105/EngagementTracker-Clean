using EngagementTracker.Models;
using Google.Cloud.Firestore;

namespace EngagementTracker.Services;

public class AttendanceService
{
    private readonly FirestoreDb _db;

    public AttendanceService(FirestoreDb db)
    {
        _db = db;
    }

    // Teacher marks attendance for a student
    public async Task<bool> MarkAttendanceAsync(
        string studentUid, string subjectId,
        string date, string status, string markedBy)
    {
        try
        {
            // Check if record already exists for this student+subject+date
            var existing = await _db.Collection("attendance")
                .WhereEqualTo("studentUid", studentUid)
                .WhereEqualTo("subjectId", subjectId)
                .WhereEqualTo("date", date)
                .GetSnapshotAsync();

            if (existing.Count > 0)
            {
                // Update existing record
                await existing.Documents[0].Reference.UpdateAsync(new Dictionary<string, object>
                {
                    { "status", status },
                    { "markedBy", markedBy }
                });
            }
            else
            {
                // Create new record
                await _db.Collection("attendance").AddAsync(new Dictionary<string, object>
                {
                    { "studentUid", studentUid },
                    { "subjectId", subjectId },
                    { "date", date },
                    { "status", status },
                    { "markedBy", markedBy }
                });
            }
            return true;
        }
        catch { return false; }
    }

    // Get attendance summary for a student in a subject
    public async Task<AttendanceSummary> GetSummaryAsync(string studentUid, string subjectId)
    {
        var snapshot = await _db.Collection("attendance")
            .WhereEqualTo("studentUid", studentUid)
            .WhereEqualTo("subjectId", subjectId)
            .GetSnapshotAsync();

        var summary = new AttendanceSummary { Total = snapshot.Count };
        foreach (var doc in snapshot.Documents)
        {
            var status = doc.GetValue<string>("status");
            if (status == "Present") summary.Present++;
            else if (status == "Absent") summary.Absent++;
            else if (status == "Late")  summary.Late++;
        }
        return summary;
    }

    // Get all attendance records for a student (for calendar)
    public async Task<List<AttendanceRecord>> GetStudentAttendanceAsync(string studentUid)
    {
        var snapshot = await _db.Collection("attendance")
            .WhereEqualTo("studentUid", studentUid)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(doc => new AttendanceRecord
        {
            Id         = doc.Id,
            StudentUid = doc.GetValue<string>("studentUid"),
            SubjectId  = doc.GetValue<string>("subjectId"),
            Date       = doc.GetValue<string>("date"),
            Status     = doc.GetValue<string>("status"),
            MarkedBy   = doc.GetValue<string>("markedBy")
        }).ToList();
    }

    // Get all students' attendance for a subject on a date (teacher view)
    public async Task<List<AttendanceRecord>> GetClassAttendanceAsync(
        string subjectId, string date)
    {
        var snapshot = await _db.Collection("attendance")
            .WhereEqualTo("subjectId", subjectId)
            .WhereEqualTo("date", date)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(doc => new AttendanceRecord
        {
            Id         = doc.Id,
            StudentUid = doc.GetValue<string>("studentUid"),
            SubjectId  = doc.GetValue<string>("subjectId"),
            Date       = doc.GetValue<string>("date"),
            Status     = doc.GetValue<string>("status"),
            MarkedBy   = doc.GetValue<string>("markedBy")
        }).ToList();
    }
}