namespace EngagementTracker.Models;

public class SectionTimetable
{
    public string Id { get; set; } = "";
    public string Section { get; set; } = "";
    public string DayOfWeek { get; set; } = ""; // E.g. "Monday"
    public string StartTime { get; set; } = ""; // E.g. "09:00"
    public string EndTime { get; set; } = "";   // E.g. "10:00"
    public string Subject { get; set; } = "";
    public string Room { get; set; } = "";
    public string TeacherName { get; set; } = "";
}
