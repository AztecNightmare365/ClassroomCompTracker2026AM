using ClassroomComputerTracker.Data;

namespace ClassroomComputerTracker;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();

            // DatabaseManager is created once and shared across the session
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dir);
            var db = new DatabaseManager(Path.Combine(dir, "ClassroomComputers.db"));

            // Login → MainForm loop: allows logout without closing the process
            while (true)
            {
                AppSession.ShouldLogout = false;

                using var login = new LoginForm(db);
                if (login.ShowDialog() != DialogResult.OK) break; // user closed the window

                using var main = new MainForm(db);
                Application.Run(main);

                if (!AppSession.ShouldLogout) break; // normal window close = exit
                // ShouldLogout == true means user clicked Logout → loop back to login
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
                $"{DateTime.Now}\n{ex}");
            MessageBox.Show(ex.ToString(), "Startup Error");
        }
    }
}
