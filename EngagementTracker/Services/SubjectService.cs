using EngagementTracker.Services;

using Google.Cloud.Firestore;
using EngagementTracker.Models;

namespace EngagementTracker.Services;

public class SubjectService
{
    private readonly FirestoreDb _db;

    public SubjectService(FirestoreDb db)
    {
        _db = db;
    }

    public async Task<List<SubjectModel>> GetSubjectsAsync()
    {
        var snap = await _db.Collection("subjects").GetSnapshotAsync();

        return snap.Documents.Select(d => new SubjectModel
        {
            Id = d.Id,
            Name = d.GetValue<string>("name"),
            Code = d.GetValue<string>("code")
        }).ToList();
    }
}