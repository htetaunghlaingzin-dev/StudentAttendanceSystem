using StudentAttendanceSystem.Forms.Shared;
using StudentAttendanceSystem.Models;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem.Forms.Admin;

public sealed class AdminDashboardForm:DashboardForm
{
    public AdminDashboardForm(AppServices s,UserSession u):base(s,u,"Admin")
    {
        AddTab("Overview",new DashboardOverviewControl(s,u));
        AddTab("Lecturers",new AdminManagementControl(s,"Teachers"));AddTab("University Classes",new AdminManagementControl(s,"Rooms"));AddTab("Courses",new AdminManagementControl(s,"Subjects"));AddTab("Lecturer Assignments",new AdminManagementControl(s,"Assignments"));AddTab("Timetable",new AdminManagementControl(s,"Timetables"));
        AddTab("Students",new StudentManagementControl(s,u));AddTab("Attendance Reports",new MonthlyReportControl(s,u));
    }
}
