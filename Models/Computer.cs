namespace ClassroomComputerTracker.Models
{
    public class Computer
    {
        public int    ComputerId       { get; set; }
        public string ComputerName     { get; set; } = "";
        public string Model            { get; set; } = "";
        public string SerialNumber     { get; set; } = "";
        public DateTime LastUpdated    { get; set; } = DateTime.Now;
        public string ConnectedItems   { get; set; } = "";
        public string Notes            { get; set; } = "";
        public int    RoomPositionX    { get; set; } = 10;
        public int    RoomPositionY    { get; set; } = 10;
        public string Status           { get; set; } = "Active"; // Active | Inactive | Broken | Retired
        public string Location         { get; set; } = "Classroom"; // Classroom | Server Room
        public string BrokenReason     { get; set; } = "";
        public bool   HotSwapAvailable { get; set; } = false;
        public string LastModifiedBy   { get; set; } = "";
    }
}
