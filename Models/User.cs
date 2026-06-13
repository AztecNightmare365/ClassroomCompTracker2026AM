namespace ClassroomComputerTracker.Models
{
    public class User
    {
        public int      UserId       { get; set; }
        public string   Username     { get; set; } = "";
        public string   PasswordHash { get; set; } = "";
        public DateTime CreatedAt    { get; set; } = DateTime.Now;
    }
}
