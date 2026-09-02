using StudentAttendanceSystem.Models;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem.Forms.Shared;

public sealed class DashboardOverviewControl : UserControl
{
    readonly AppServices services; readonly UserSession session;
    readonly Label students=ValueLabel(), classes=ValueLabel(), courses=ValueLabel(), attendance=ValueLabel();
    public DashboardOverviewControl(AppServices services,UserSession session)
    {
        this.services=services;this.session=session;BackColor=Ui.Background;
        var welcome=new Label{Text=$"Welcome back, {session.FullName}",AutoSize=true,Font=new Font("Segoe UI Semibold",20),ForeColor=Ui.Text,Location=new Point(8,8)};
        var hint=new Label{Text=session.Role==UserRole.Admin?"Here is a concise overview of the university attendance system.":"Your classes, students and attendance tools are ready.",AutoSize=true,Font=new Font("Segoe UI",10),ForeColor=Ui.Muted,Location=new Point(10,48)};
        var cards=new TableLayoutPanel{Location=new Point(0,84),Height=132,Dock=DockStyle.Top,ColumnCount=4,Padding=new Padding(0,8,0,8)};for(var i=0;i<4;i++)cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
        cards.Controls.Add(Kpi("Active students",students,"ST"),0,0);cards.Controls.Add(Kpi("Accessible classes",classes,"CL"),1,0);cards.Controls.Add(Kpi("Active courses",courses,"CO"),2,0);cards.Controls.Add(Kpi("Records this month",attendance,"AT"),3,0);
        var note=Ui.Card(24);note.Dock=DockStyle.Top;note.Height=150;note.Controls.Add(new Label{Text=session.Role==UserRole.Teacher?"Quick start\n\nChoose Take Attendance, select an assigned class and course, confirm the date, then mark each student. Home-room access remains read-only where required.":"Quick start\n\nUse the navigation to manage lecturers, classes, courses and assignments. Attendance reports keep the same role-aware access rules.",AutoSize=false,Dock=DockStyle.Fill,Font=new Font("Segoe UI",10),ForeColor=Ui.Text});
        Controls.Add(note);Controls.Add(cards);Controls.Add(hint);Controls.Add(welcome);Load+=(_,_)=>Ui.Safe(LoadAsync);
    }
    static Label ValueLabel()=>new(){Text="—",AutoSize=true,Font=new Font("Segoe UI Semibold",24),ForeColor=Ui.Text,Location=new Point(22,40)};
    static Panel Kpi(string title,Label value,string mark){var p=Ui.Card();p.Dock=DockStyle.Fill;p.Margin=new Padding(8);p.Controls.Add(new Label{Text=mark,TextAlign=ContentAlignment.MiddleCenter,Size=new Size(38,38),Location=new Point(18,17),BackColor=Ui.AccentLight,ForeColor=Ui.Accent,Font=new Font("Segoe UI Semibold",9)});value.Location=new Point(66,15);p.Controls.Add(value);p.Controls.Add(new Label{Text=title,AutoSize=true,ForeColor=Ui.Muted,Font=new Font("Segoe UI",9),Location=new Point(68,61)});return p;}
    async Task LoadAsync(){var rooms=await services.Attendance.GetAccessibleRoomsAsync(session);classes.Text=rooms.Count.ToString();students.Text=rooms.Count==0?"0":(await services.Db.QuerySingleAsync("SELECT COUNT(*) FROM Students WHERE IsActive=1",r=>r.GetInt32(0))).ToString();courses.Text=(await services.Db.QuerySingleAsync("SELECT COUNT(*) FROM Subjects WHERE IsActive=1",r=>r.GetInt32(0))).ToString();attendance.Text=(await services.Db.QuerySingleAsync("SELECT COUNT(*) FROM Attendance WHERE AttendanceDate>=DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1)",r=>r.GetInt32(0))).ToString();}
}
