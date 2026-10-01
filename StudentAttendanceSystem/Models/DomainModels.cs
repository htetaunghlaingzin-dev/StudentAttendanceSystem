namespace StudentAttendanceSystem.Models;

public enum UserRole { Admin, Teacher }
public enum AttendanceStatus { Present, Absent }

public sealed record UserSession(int UserId, int? TeacherId, string Username, string FullName, UserRole Role);
public sealed record LookupItem(int Id, string Name) { public override string ToString() => Name; }
public sealed record StudentItem(int StudentId, string StudentCode, string StudentName, string Gender, int RoomId, string RoomName, bool IsActive);
public sealed record AttendanceRow(int StudentId, string StudentCode, string StudentName, AttendanceStatus Status, string? Remark);
public sealed record AttendanceAssignment(int RoomId,int SubjectId,string ClassName,string CourseName)
{
    public string DisplayName=>$"{ClassName}\r\n{CourseName}";
    public override string ToString()=>$"{ClassName}  •  {CourseName}";
}
public sealed record ScheduledAttendanceSession(int SessionId,int RoomId,int SubjectId,string ClassName,string CourseName,TimeSpan StartTime,TimeSpan EndTime,string Status)
{
    public string TimeLabel=>$"{DateTime.Today.Add(StartTime):h:mm tt} - {DateTime.Today.Add(EndTime):h:mm tt}";
}
public sealed record MonthlyReportRow(string StudentCode, string StudentName, string Room, int TotalSessions,
    int Present, int Absent, decimal AttendancePercentage, string Remark);
