namespace EngagementTracker.Models;

public class AssignmentModel
{
    public string Id          { get; set; } = "";
    public string Title       { get; set; } = "";
    public string Description { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherUid  { get; set; } = "";
    public string DueDate     { get; set; } = "";
    public double MaxMarks    { get; set; }
    public string CreatedAt   { get; set; } = "";
}

public class SubmissionModel
{
    public string Id           { get; set; } = "";
    public string AssignmentId { get; set; } = "";
    public string StudentUid   { get; set; } = "";
    public string StudentName  { get; set; } = "";
    public string FileUrl      { get; set; } = "";
    public string FileName     { get; set; } = "";
    public string SubmittedAt  { get; set; } = "";
    public double Grade        { get; set; }
    public string Feedback     { get; set; } = "";
    public bool   IsGraded     { get; set; }
}