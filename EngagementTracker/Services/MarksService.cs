using EngagementTracker.Models;
using Google.Cloud.Firestore;

namespace EngagementTracker.Services;

public class MarksService
{
    private readonly FirestoreDb _db;

    public MarksService(FirestoreDb db)
    {
        _db = db;
    }

    // Auto-calculate grade and GPA from percentage
    public static (string grade, double gpa) CalculateGrade(double percentage)
    {
        return percentage switch
        {
            >= 90 => ("A+", 10.0),
            >= 80 => ("A",   9.0),
            >= 70 => ("B+",  8.0),
            >= 60 => ("B",   7.0),
            >= 50 => ("C",   6.0),
            _     => ("F",   0.0)
        };
    }

    // Teacher saves marks for a student
    public async Task<bool> SaveMarksAsync(
        string studentUid, string subjectId, string subjectName,
        string examType, double score, double maxScore)
    {
        try
        {
            var percentage = maxScore == 0 ? 0 : score * 100.0 / maxScore;
            var (grade, gpa) = CalculateGrade(percentage);

            var existing = await _db.Collection("marks")
                .WhereEqualTo("studentUid", studentUid)
                .WhereEqualTo("subjectId", subjectId)
                .WhereEqualTo("examType", examType)
                .GetSnapshotAsync();

            var data = new Dictionary<string, object>
            {
                { "studentUid",  studentUid  },
                { "subjectId",   subjectId   },
                { "subjectName", subjectName },
                { "examType",    examType    },
                { "score",       score       },
                { "maxScore",    maxScore    },
                { "grade",       grade       },
                { "gpaPoints",   gpa         }
            };

            if (existing.Count > 0)
                await existing.Documents[0].Reference.SetAsync(data);
            else
                await _db.Collection("marks").AddAsync(data);

            return true;
        }
        catch { return false; }
    }

    // Teacher saves bulk marks (for Quiz)
    public async Task<bool> SaveBulkMarksAsync(List<MarksRecord> submissions)
    {
        try
        {
            var batch = _db.StartBatch();
            foreach (var s in submissions)
            {
                var percentage = s.MaxScore == 0 ? 0 : s.Score * 100.0 / s.MaxScore;
                var (grade, gpa) = CalculateGrade(percentage);

                var data = new Dictionary<string, object>
                {
                    { "studentUid",  s.StudentUid  },
                    { "subjectId",   s.SubjectId ?? s.SubjectName },
                    { "subjectName", s.SubjectName },
                    { "examType",    s.ExamType    },
                    { "score",       s.Score       },
                    { "maxScore",    s.MaxScore    },
                    { "grade",       grade       },
                    { "gpaPoints",   gpa         }
                };

                var existing = await _db.Collection("marks")
                    .WhereEqualTo("studentUid", s.StudentUid)
                    .WhereEqualTo("subjectId", s.SubjectId ?? s.SubjectName)
                    .WhereEqualTo("examType", s.ExamType)
                    .GetSnapshotAsync();

                if (existing.Count > 0)
                    batch.Set(existing.Documents[0].Reference, data);
                else
                    batch.Create(_db.Collection("marks").Document(), data);
            }
            await batch.CommitAsync();
            return true;
        }
        catch { return false; }
    }

    // Get all marks for a student
    public async Task<List<MarksRecord>> GetStudentMarksAsync(string studentUid)
    {
        var snapshot = await _db.Collection("marks")
            .WhereEqualTo("studentUid", studentUid)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(doc => new MarksRecord
        {
            Id          = doc.Id,
            StudentUid  = doc.GetValue<string>("studentUid"),
            SubjectId   = doc.GetValue<string>("subjectId"),
            SubjectName = doc.GetValue<string>("subjectName"),
            ExamType    = doc.GetValue<string>("examType"),
            Score       = doc.GetValue<double>("score"),
            MaxScore    = doc.GetValue<double>("maxScore"),
            Grade       = doc.GetValue<string>("grade"),
            GpaPoints   = doc.GetValue<double>("gpaPoints")
        }).ToList();
    }

    // Calculate subject-wise GPA summary
    public List<SubjectGPA> GetSubjectGPAs(List<MarksRecord> marks)
    {
        return marks
            .GroupBy(m => m.SubjectId)
            .Select(g =>
            {
                var avg = g.Average(m => m.GpaPoints);
                var (grade, _) = CalculateGrade(g.Average(m => m.Percentage));
                return new SubjectGPA
                {
                    SubjectId   = g.Key,
                    SubjectName = g.First().SubjectName,
                    AverageGPA  = Math.Round(avg, 2),
                    Grade       = grade
                };
            }).ToList();
    }
}