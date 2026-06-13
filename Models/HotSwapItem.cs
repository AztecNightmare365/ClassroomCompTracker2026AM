namespace ClassroomComputerTracker.Models
{
    public class HotSwapItem
    {
        public int      HotSwapItemId  { get; set; }
        public string   ItemName       { get; set; } = "";
        public string   ItemType       { get; set; } = "Monitor"; // Monitor | Keyboard | Mouse | Other
        public string   SerialNumber   { get; set; } = "";
        public string   Location       { get; set; } = "";
        public string   Condition      { get; set; } = "Good";   // Good | Fair | Poor
        public bool     Available      { get; set; } = true;
        public string   Notes          { get; set; } = "";
        public DateTime LastUpdated    { get; set; } = DateTime.Now;
        public string   LastModifiedBy { get; set; } = "";
    }
}
