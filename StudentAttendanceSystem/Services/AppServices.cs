using StudentAttendanceSystem.Data;

namespace StudentAttendanceSystem.Services;

public sealed class AppServices
{
    public Database Db { get; }
    public AuthenticationService Authentication { get; }
    public AuthorizationService Authorization { get; }
    public AttendanceService Attendance { get; }
    public AdminService Admin { get; }
    public StudentService Students { get; }
    public AppServices(Database db)
    {
        Db = db;
        Authorization = new AuthorizationService(db);
        Authentication = new AuthenticationService(db);
        Attendance = new AttendanceService(db, Authorization);
        Admin = new AdminService(db);
        Students = new StudentService(db, Authorization);
    }
}
