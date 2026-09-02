using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Forms.Auth;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var db = new Database();
            var services = new AppServices(db);
            Application.Run(new LoginForm(services));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The application could not start.\n\n{ex.Message}\n\nRun Database/Setup.sql and verify appsettings.json.",
                "Startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
