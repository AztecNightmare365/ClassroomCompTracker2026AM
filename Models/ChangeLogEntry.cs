namespace ClassroomComputerTracker.Models
{
    public class ChangeLogEntry
    {
        public int      LogId      { get; set; }
        public string   Username   { get; set; } = "";
        public string   Action     { get; set; } = "";
        public string   EntityType { get; set; } = "";
        public int      EntityId   { get; set; }
        public string   EntityName { get; set; } = "";
        public string   Detail     { get; set; } = "";
        public DateTime Timestamp  { get; set; } = DateTime.Now;
    }
}
