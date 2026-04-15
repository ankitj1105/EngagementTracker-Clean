using EngagementTracker.Models;
using Google.Cloud.Firestore;

namespace EngagementTracker.Services;

public class TimetableService
{
    private readonly FirestoreDb _db;

    public TimetableService(FirestoreDb db)
    {
        _db = db;
    }

    public async Task<List<SectionTimetable>> GetTimetableAsync(string section)
    {
        var snap = await _db.Collection("timetables")
            .WhereEqualTo("section", section)
            .GetSnapshotAsync();

        return snap.Documents.Select(doc => new SectionTimetable
        {
            Id = doc.Id,
            Section = doc.GetValue<string>("section"),
            DayOfWeek = doc.GetValue<string>("dayOfWeek"),
            StartTime = doc.GetValue<string>("startTime"),
            EndTime = doc.GetValue<string>("endTime"),
            Subject = doc.GetValue<string>("subject"),
            Room = doc.ContainsField("room") ? doc.GetValue<string>("room") : "",
            TeacherName = doc.ContainsField("teacherName") ? doc.GetValue<string>("teacherName") : ""
        }).ToList();
    }

    public async Task<bool> SaveTimetableAsync(string section, List<SectionTimetable> entries)
    {
        try
        {
            var existing = await _db.Collection("timetables")
                .WhereEqualTo("section", section)
                .GetSnapshotAsync();

            var batch = _db.StartBatch();
            foreach (var doc in existing.Documents)
            {
                batch.Delete(doc.Reference);
            }

            foreach (var e in entries)
            {
                var data = new Dictionary<string, object>
                {
                    { "section", section },
                    { "dayOfWeek", e.DayOfWeek },
                    { "startTime", e.StartTime },
                    { "endTime", e.EndTime },
                    { "subject", e.Subject },
                    { "room", e.Room ?? "" },
                    { "teacherName", e.TeacherName ?? "" }
                };
                batch.Create(_db.Collection("timetables").Document(), data);
            }

            await batch.CommitAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
