namespace Attendly.Domain;

public sealed record UserAccount(int UserId,string Username,string FullName,string Role,int? TeacherId);
public sealed record Student(int StudentId,string StudentCode,string StudentName,string Gender,int RoomId,string RoomName,bool IsActive);
public sealed record AttendanceEntry(int StudentId,string StudentCode,string StudentName,string Status,string? Remark);
