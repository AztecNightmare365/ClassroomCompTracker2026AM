namespace ClassroomComputerTracker;

public static class AppSession
{
    public static int    UserId       { get; set; } = 0;
    public static string Username     { get; set; } = "";
    public static bool   ShouldLogout { get; set; } = false;
    public static bool   IsLoggedIn   => UserId > 0;
}
