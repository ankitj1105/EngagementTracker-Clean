namespace EngagementTracker.Models;

public class AttendanceRecord
{
    public string Id        { get; set; } = "";
    public string StudentUid { get; set; } = "";
    public string SubjectId  { get; set; } = "";
    public string Date       { get; set; } = ""; // format: "yyyy-MM-dd"
    public string Status     { get; set; } = ""; // "Present" | "Absent" | "Late"
    public string MarkedBy   { get; set; } = ""; // teacher uid
}

public class AttendanceSummary
{
    public int    Total    { get; set; }
    public int    Present  { get; set; }
    public int    Absent   { get; set; }
    public int    Late     { get; set; }
    public double Percentage => Total == 0 ? 0 : Math.Round((Present + Late) * 100.0 / Total, 1);
    public bool   IsAtRisk   => Percentage < 75;
}