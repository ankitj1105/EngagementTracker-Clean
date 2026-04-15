namespace EngagementTracker.Models;

public class MarksRecord
{
    public string Id        { get; set; } = "";
    public string StudentUid { get; set; } = "";
    public string SubjectId  { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string ExamType   { get; set; } = ""; // "Midterm" | "EndTerm" | "Quiz"
    public double Score      { get; set; }
    public double MaxScore   { get; set; }
    public string Grade      { get; set; } = "";
    public double GpaPoints  { get; set; }

    public double Percentage => MaxScore == 0 ? 0 : Math.Round(Score * 100.0 / MaxScore, 1);
}

public class SubjectGPA
{
    public string SubjectId   { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public double AverageGPA  { get; set; }
    public string Grade       { get; set; } = "";
    public bool   IsAtRisk    => AverageGPA < 5.0;
}