namespace EngagementTracker.Models
{
    public class LiveClassModel
    {
        public string Subject { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class LiveSession
    {
        public string Id { get; set; } = "";
        public string TeacherUid { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Date { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsLive { get; set; }
        public int DurationMinutes { get; set; } = 60;
        // Student must attend 80% of class to be auto-marked Present
        public int AttendanceThresholdPercent { get; set; } = 80;
    }

    public class StudentSessionLog
    {
        public string Id { get; set; } = "";
        public string SessionId { get; set; } = "";
        public string StudentUid { get; set; } = "";
        public string StudentName { get; set; } = "";
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public double MinutesPresent { get; set; }
        public bool AutoMarked { get; set; }
    }

    public class LiveClassViewModel
    {
        public LiveSession? ActiveSession { get; set; }
        public bool IsAlreadyMarked { get; set; }
        public double MinutesSpent { get; set; }
    }
}