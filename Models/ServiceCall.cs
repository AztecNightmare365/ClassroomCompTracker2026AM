namespace ClassroomComputerTracker.Models
{
    public class ServiceCall
    {
        public int       ServiceCallId  { get; set; }
        public int       ComputerId     { get; set; }
        public string    ComputerName   { get; set; } = "";
        public string    Description    { get; set; } = "";
        public string    Status         { get; set; } = "Open"; // Open | In Progress | Resolved
        public string    SolutionNotes  { get; set; } = "";
        public DateTime  OpenedAt       { get; set; } = DateTime.Now;
        public DateTime? ClosedAt       { get; set; }
        public string    OpenedByUser   { get; set; } = "";
        public string    ResolvedByUser { get; set; } = "";
    }
}
