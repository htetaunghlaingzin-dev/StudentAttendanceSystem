using StudentAttendanceSystem.Forms.Shared;
using StudentAttendanceSystem.Models;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem.Forms.Teacher;

public sealed class TeacherDashboardForm:DashboardForm
{
    public TeacherDashboardForm(AppServices s,UserSession u):base(s,u,"Lecturer")
    {AddTab("Overview",new DashboardOverviewControl(s,u));AddTab("Students",new StudentManagementControl(s,u));AddTab("Take Attendance",new AttendanceControl(s,u));AddTab("Attendance History",new AttendanceHistoryControl(s,u));AddTab("Monthly Reports",new MonthlyReportControl(s,u));Shown+=(_,_)=>Ui.Safe(AddHomeRoomAsync);}
    async Task AddHomeRoomAsync(){if((await Services.Attendance.GetHomeRoomsAsync(Session)).Count>0)AddTab("Home Room Overall",new MonthlyReportControl(Services,Session,true));}
}
