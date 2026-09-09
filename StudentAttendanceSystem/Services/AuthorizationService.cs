using Microsoft.Data.SqlClient;
using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Models;

namespace StudentAttendanceSystem.Services;

public sealed class AuthorizationService(Database db)
{
    private async Task<bool> Exists(string sql, params SqlParameter[] p) => (await db.QueryAsync(sql, _ => true, p)).Any();
    public Task<bool> IsHomeRoomTeacherAsync(int teacherId, int roomId) => Exists("SELECT 1 FROM Rooms WHERE RoomId=@r AND HomeRoomTeacherId=@t AND IsActive=1", new("@r",roomId),new("@t",teacherId));
    public Task<bool> CanTakeAttendanceAsync(int teacherId, int roomId, int subjectId) => Exists("SELECT 1 FROM TeacherAssignments ta JOIN Teachers t ON t.TeacherId=ta.TeacherId JOIN Users u ON u.UserId=t.UserId JOIN Rooms r ON r.RoomId=ta.RoomId JOIN Subjects s ON s.SubjectId=ta.SubjectId WHERE ta.TeacherId=@t AND ta.RoomId=@r AND ta.SubjectId=@s AND ta.IsActive=1 AND u.IsActive=1 AND r.IsActive=1 AND s.IsActive=1",new("@t",teacherId),new("@r",roomId),new("@s",subjectId));
    public Task<bool> CanManageStudentsAsync(int teacherId, int roomId) => IsHomeRoomTeacherAsync(teacherId,roomId);
    public async Task<bool> CanViewStudentsAsync(int teacherId,int roomId) => await IsHomeRoomTeacherAsync(teacherId,roomId) || await Exists("SELECT 1 FROM TeacherAssignments ta JOIN Rooms r ON r.RoomId=ta.RoomId WHERE ta.TeacherId=@t AND ta.RoomId=@r AND ta.IsActive=1 AND r.IsActive=1",new("@t",teacherId),new("@r",roomId));
    public async Task<bool> CanViewAttendanceAsync(int teacherId, int roomId, int subjectId) => await IsHomeRoomTeacherAsync(teacherId,roomId) || await CanTakeAttendanceAsync(teacherId,roomId,subjectId);
    public async Task<bool> CanEditAttendanceAsync(int teacherId, int attendanceId)
    {
        var records=await db.QueryAsync("SELECT RoomId,SubjectId,TeacherId FROM Attendance WHERE AttendanceId=@a",r=>new { Room=r.GetInt32(0),Subject=r.GetInt32(1),Owner=r.GetInt32(2)},new SqlParameter("@a",attendanceId));
        var a=records.SingleOrDefault();
        return a is not null && a.Owner==teacherId && await CanTakeAttendanceAsync(teacherId,a.Room,a.Subject);
    }

    public async Task EnsureCanTakeAttendanceAsync(UserSession session,int roomId,int subjectId)
    {
        if(session.Role==UserRole.Admin) return;
        if(session.TeacherId is null || !await CanTakeAttendanceAsync(session.TeacherId.Value,roomId,subjectId)) throw new UnauthorizedAccessException("You are not assigned to this room and subject.");
    }
    public async Task EnsureCanManageStudentsAsync(UserSession session,int roomId)
    {
        if(session.Role==UserRole.Admin) return;
        if(session.TeacherId is null || !await CanManageStudentsAsync(session.TeacherId.Value,roomId)) throw new UnauthorizedAccessException("Only this class's home-room lecturer can manage its students.");
    }
    public async Task EnsureCanViewStudentsAsync(UserSession session,int roomId)
    {
        if(session.Role==UserRole.Admin)return;
        if(session.TeacherId is null||!await CanViewStudentsAsync(session.TeacherId.Value,roomId))throw new UnauthorizedAccessException("You cannot view students in this class.");
    }
}
