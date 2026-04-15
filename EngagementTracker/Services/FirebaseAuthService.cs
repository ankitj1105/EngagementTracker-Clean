using Google.Cloud.Firestore;

namespace EngagementTracker.Services;

public class FirebaseAuthService
{
    private readonly FirestoreDb _db;

    public FirebaseAuthService(FirestoreDb db)
    {
        _db = db;
    }

    public async Task<(bool success, string message)> SaveProfileAsync(
        string uid, string name, string email,
        string role, string section, string rollNo)
    {
        try
        {
            var docRef = _db.Collection("users").Document(uid);
            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "name", name },
                { "email", email },
                { "role", role },
                { "section", section },
                { "rollNo", rollNo },
                { "createdAt", Timestamp.GetCurrentTimestamp() }
            });
            return (true, "Profile saved");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string name, string role)> GetUserProfileAsync(string uid)
    {
        try
        {
            var docRef = _db.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
                return (false, "", "");
            var name = snapshot.GetValue<string>("name");
            var role = snapshot.GetValue<string>("role");
            return (true, name, role);
        }
        catch
        {
            return (false, "", "");
        }
    }
}