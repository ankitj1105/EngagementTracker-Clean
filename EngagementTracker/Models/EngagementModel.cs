namespace EngagementTracker.Models;

public class EngagementLog
{
    public string Id              { get; set; } = "";
    public string StudentUid      { get; set; } = "";
    public string StudentName     { get; set; } = "";
    public int    EngagementScore { get; set; }
    public string SessionId       { get; set; } = "";
    public string Timestamp       { get; set; } = "";
    public bool   FaceDetected    { get; set; }
}

public class EngagementSummary
{
    public string StudentUid      { get; set; } = "";
    public string StudentName     { get; set; } = "";
    public double AverageScore    { get; set; }
    public int    TotalSessions   { get; set; }
    public string LastActive      { get; set; } = "";
    public string Status          => AverageScore >= 70 ? "High" : AverageScore >= 40 ? "Medium" : "Low";
}