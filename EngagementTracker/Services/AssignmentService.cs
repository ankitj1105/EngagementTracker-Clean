using EngagementTracker.Models;
using Google.Cloud.Firestore;

namespace EngagementTracker.Services;

public class AssignmentService
{
    private readonly FirestoreDb _db;
    public AssignmentService(FirestoreDb db) { _db = db; }

    // Teacher creates assignment
    public async Task<bool> CreateAssignmentAsync(AssignmentModel a)
    {
        try
        {
            await _db.Collection("assignments").AddAsync(new Dictionary<string, object>
            {
                { "title",       a.Title       },
                { "description", a.Description },
                { "subjectName", a.SubjectName },
                { "teacherUid",  a.TeacherUid  },
                { "dueDate",     a.DueDate     },
                { "maxMarks",    a.MaxMarks    },
                { "createdAt",   DateTime.UtcNow.ToString("yyyy-MM-dd") }
            });
            return true;
        }
        catch { return false; }
    }

    // Get all assignments
    public async Task<List<AssignmentModel>> GetAllAssignmentsAsync()
    {
        try
        {
            var snap = await _db.Collection("assignments")
                .OrderByDescending("createdAt")
                .GetSnapshotAsync();
            return snap.Documents.Select(d => new AssignmentModel
            {
                Id          = d.Id,
                Title       = d.ContainsField("title") ? d.GetValue<string>("title") : "",
                Description = d.ContainsField("description") ? d.GetValue<string>("description") : "",
                SubjectName = d.ContainsField("subjectName") ? d.GetValue<string>("subjectName") : "",
                TeacherUid  = d.ContainsField("teacherUid") ? d.GetValue<string>("teacherUid") : "",
                DueDate     = d.ContainsField("dueDate") ? d.GetValue<string>("dueDate") : "",
                MaxMarks    = d.ContainsField("maxMarks") ? Convert.ToDouble(d.GetValue<object>("maxMarks")) : 0.0,
                CreatedAt   = d.ContainsField("createdAt") ? d.GetValue<string>("createdAt") : ""
            }).ToList();
        }
        catch { return new List<AssignmentModel>(); }
    }

    // Student submits assignment (text/link based — no file upload needed)
    public async Task<bool> SubmitAssignmentAsync(SubmissionModel s)
    {
        try
        {
            // Check if already submitted
            var existing = await _db.Collection("submissions")
                .WhereEqualTo("assignmentId", s.AssignmentId)
                .WhereEqualTo("studentUid",   s.StudentUid)
                .GetSnapshotAsync();

            if (existing.Count > 0)
            {
                await existing.Documents[0].Reference.UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "fileUrl",     s.FileUrl     },
                        { "fileName",    s.FileName    },
                        { "submittedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") }
                    });
            }
            else
            {
                await _db.Collection("submissions").AddAsync(new Dictionary<string, object>
                {
                    { "assignmentId", s.AssignmentId },
                    { "studentUid",   s.StudentUid   },
                    { "studentName",  s.StudentName  },
                    { "fileUrl",      s.FileUrl      },
                    { "fileName",     s.FileName     },
                    { "submittedAt",  DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
                    { "grade",        0.0            },
                    { "feedback",     ""             },
                    { "isGraded",     false          }
                });
            }
            return true;
        }
        catch { return false; }
    }

    // Get submissions for an assignment (teacher view)
    public async Task<List<SubmissionModel>> GetSubmissionsAsync(string assignmentId)
    {
        try
        {
            var snap = await _db.Collection("submissions")
                .WhereEqualTo("assignmentId", assignmentId)
                .GetSnapshotAsync();
            return snap.Documents.Select(d => new SubmissionModel
            {
                Id           = d.Id,
                AssignmentId = d.ContainsField("assignmentId") ? d.GetValue<string>("assignmentId") : "",
                StudentUid   = d.ContainsField("studentUid") ? d.GetValue<string>("studentUid") : "",
                StudentName  = d.ContainsField("studentName") ? d.GetValue<string>("studentName") : "",
                FileUrl      = d.ContainsField("fileUrl") ? d.GetValue<string>("fileUrl") : "",
                FileName     = d.ContainsField("fileName") ? d.GetValue<string>("fileName") : "",
                SubmittedAt  = d.ContainsField("submittedAt") ? d.GetValue<string>("submittedAt") : "",
                Grade        = d.ContainsField("grade") ? Convert.ToDouble(d.GetValue<object>("grade")) : 0.0,
                Feedback     = d.ContainsField("feedback") ? d.GetValue<string>("feedback") : "",
                IsGraded     = d.ContainsField("isGraded") && d.GetValue<bool>("isGraded")
            }).ToList();
        }
        catch { return new List<SubmissionModel>(); }
    }

    // Get student's own submissions
    public async Task<List<SubmissionModel>> GetMySubmissionsAsync(string studentUid)
    {
        try
        {
            var snap = await _db.Collection("submissions")
                .WhereEqualTo("studentUid", studentUid)
                .GetSnapshotAsync();
            return snap.Documents.Select(d => new SubmissionModel
            {
                Id           = d.Id,
                AssignmentId = d.ContainsField("assignmentId") ? d.GetValue<string>("assignmentId") : "",
                StudentUid   = d.ContainsField("studentUid") ? d.GetValue<string>("studentUid") : "",
                FileUrl      = d.ContainsField("fileUrl") ? d.GetValue<string>("fileUrl") : "",
                FileName     = d.ContainsField("fileName") ? d.GetValue<string>("fileName") : "",
                SubmittedAt  = d.ContainsField("submittedAt") ? d.GetValue<string>("submittedAt") : "",
                Grade        = d.ContainsField("grade") ? Convert.ToDouble(d.GetValue<object>("grade")) : 0.0,
                Feedback     = d.ContainsField("feedback") ? d.GetValue<string>("feedback") : "",
                IsGraded     = d.ContainsField("isGraded") && d.GetValue<bool>("isGraded")
            }).ToList();
        }
        catch { return new List<SubmissionModel>(); }
    }

    // Teacher grades a submission
    public async Task<bool> GradeSubmissionAsync(string submissionId, double grade, string feedback)
    {
        try
        {
            await _db.Collection("submissions").Document(submissionId).UpdateAsync(
                new Dictionary<string, object>
                {
                    { "grade",    grade    },
                    { "feedback", feedback },
                    { "isGraded", true     }
                });
            return true;
        }
        catch { return false; }
    }
}