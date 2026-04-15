namespace EngagementTracker.Models;

public class LiveAttendanceModel
{
    public string StudentUid { get; set; } = "";
    public double TotalSeconds { get; set; } = 0;
    public bool IsPresent { get; set; } = false;
}