using Microsoft.Data.SqlClient;
using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Models;

namespace StudentAttendanceSystem.Services;

public sealed class StudentService(Database db,AuthorizationService authorization)
{
    public async Task<List<StudentItem>> GetAsync(UserSession session,int roomId)
    {
        await authorization.EnsureCanManageStudentsAsync(session,roomId);
        return await db.QueryAsync<StudentItem>("SELECT s.StudentId,s.StudentCode,s.StudentName,COALESCE(s.Gender,''),s.RoomId,r.RoomName,s.IsActive FROM Students s JOIN Rooms r ON r.RoomId=s.RoomId WHERE s.RoomId=@r ORDER BY s.StudentId",r=>new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetInt32(4),r.GetString(5),r.GetBoolean(6)),new SqlParameter("@r",roomId));
    }
    public async Task SaveAsync(UserSession session,int? id,string code,string name,string gender,int roomId,bool active)
    {
        await authorization.EnsureCanManageStudentsAsync(session,roomId);
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Student code and name are required.");
        if(id is null) await db.ExecuteAsync("INSERT Students(StudentCode,StudentName,Gender,RoomId,AcademicYear,IsActive) SELECT @c,@n,@g,RoomId,AcademicYear,@a FROM Rooms WHERE RoomId=@r",new("@c",code.Trim()),new("@n",name.Trim()),new("@g",gender),new("@r",roomId),new("@a",active));
        else await db.ExecuteAsync("UPDATE Students SET StudentCode=@c,StudentName=@n,Gender=@g,IsActive=@a WHERE StudentId=@id AND RoomId=@r",new("@c",code.Trim()),new("@n",name.Trim()),new("@g",gender),new("@a",active),new("@id",id),new("@r",roomId));
    }
    public async Task SetActiveAsync(UserSession session,int studentId,int roomId,bool active)
    {
        await authorization.EnsureCanManageStudentsAsync(session,roomId);
        if(await db.ExecuteAsync("UPDATE Students SET IsActive=@a WHERE StudentId=@id AND RoomId=@r",new("@a",active),new("@id",studentId),new("@r",roomId))==0)throw new InvalidOperationException("Student was not found in this class.");
    }
}
