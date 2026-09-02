using Microsoft.Data.SqlClient;
using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Helpers;
using StudentAttendanceSystem.Models;

namespace StudentAttendanceSystem.Services;

public sealed class AdminService(Database db)
{
    public Task<List<LookupItem>> GetRoomsAsync(bool activeOnly=true)=>db.QueryAsync<LookupItem>($"SELECT RoomId,ProgramName+' | '+RoomName+' | '+StudyLevel+' | Sem '+CAST(Semester AS varchar(1))+' | '+AcademicYear FROM Rooms{(activeOnly?" WHERE IsActive=1":"")} ORDER BY RoomId",r=>new(r.GetInt32(0),r.GetString(1)));
    public Task<List<LookupItem>> GetSubjectsAsync(bool activeOnly=true)=>db.QueryAsync<LookupItem>($"SELECT SubjectId,SubjectCode+' - '+SubjectName FROM Subjects{(activeOnly?" WHERE IsActive=1":"")} ORDER BY SubjectId",r=>new(r.GetInt32(0),r.GetString(1)));
    public Task<List<LookupItem>> GetTeachersAsync(bool activeOnly=true)=>db.QueryAsync<LookupItem>($"SELECT t.TeacherId,t.TeacherCode+' - '+t.FullName FROM Teachers t JOIN Users u ON u.UserId=t.UserId{(activeOnly?" WHERE u.IsActive=1":"")} ORDER BY t.TeacherId",r=>new(r.GetInt32(0),r.GetString(1)));

    public async Task CreateTeacherAsync(string username,string password,string code,string fullName,string phone)
    {
        if(string.IsNullOrWhiteSpace(username)||password.Length<8||string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Username, teacher code and name are required; password must be at least 8 characters.");
        await using var c=db.CreateConnection(); await c.OpenAsync(); await using var tx=await c.BeginTransactionAsync();
        try {
            var cmd=new SqlCommand("INSERT Users(Username,PasswordHash,FullName,Role) OUTPUT INSERTED.UserId VALUES(@u,@p,@n,'Teacher')",c,(SqlTransaction)tx);
            cmd.Parameters.AddRange([new("@u",username.Trim()),new("@p",PasswordHasher.Hash(password)),new("@n",fullName.Trim())]);
            var userId=(int)(await cmd.ExecuteScalarAsync()??throw new InvalidOperationException());
            cmd=new SqlCommand("INSERT Teachers(UserId,TeacherCode,FullName,Phone) VALUES(@u,@c,@n,@p)",c,(SqlTransaction)tx);
            cmd.Parameters.AddRange([new("@u",userId),new("@c",code.Trim()),new("@n",fullName.Trim()),new("@p",(object?)phone.Trim()??DBNull.Value)]); await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        } catch { await tx.RollbackAsync(); throw; }
    }
    public Task ResetTeacherPasswordAsync(int teacherId,string password)
    {
        if(password.Length<8) throw new ArgumentException("Password must be at least 8 characters.");
        return db.ExecuteAsync("UPDATE u SET PasswordHash=@p FROM Users u JOIN Teachers t ON t.UserId=u.UserId WHERE t.TeacherId=@t",new("@p",PasswordHasher.Hash(password)),new("@t",teacherId));
    }
    public async Task UpdateTeacherAsync(int teacherId,string code,string fullName,string phone)
    {
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Lecturer code and full name are required.");
        await using var c=db.CreateConnection();await c.OpenAsync();await using var tx=await c.BeginTransactionAsync();
        try{var cmd=new SqlCommand("UPDATE t SET TeacherCode=@c,FullName=@n,Phone=@p FROM Teachers t WHERE TeacherId=@id; UPDATE u SET FullName=@n FROM Users u JOIN Teachers t ON t.UserId=u.UserId WHERE t.TeacherId=@id",c,(SqlTransaction)tx);cmd.Parameters.AddRange([new("@c",code.Trim()),new("@n",fullName.Trim()),new("@p",string.IsNullOrWhiteSpace(phone)?DBNull.Value:phone.Trim()),new("@id",teacherId)]);if(await cmd.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Lecturer was not found.");await tx.CommitAsync();}catch{await tx.RollbackAsync();throw;}
    }
    public Task SetTeacherActiveAsync(int teacherId,bool active)=>db.ExecuteAsync("UPDATE u SET IsActive=@a FROM Users u JOIN Teachers t ON t.UserId=u.UserId WHERE t.TeacherId=@t",new("@a",active),new("@t",teacherId));
    public Task SaveRoomAsync(int? id,string name,string department,string program,string studyLevel,int semester,string year,int? homeTeacher,bool active)
    {
        if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(department)||string.IsNullOrWhiteSpace(program)||string.IsNullOrWhiteSpace(studyLevel)||string.IsNullOrWhiteSpace(year)) throw new ArgumentException("Class/section, department, program, study level and academic year are required.");
        if(semester is <1 or >10) throw new ArgumentException("Semester must be between 1 and 10.");
        return id is null
            ?db.ExecuteAsync("INSERT Rooms(RoomName,Department,ProgramName,StudyLevel,Semester,AcademicYear,HomeRoomTeacherId,IsActive) VALUES(@n,@d,@p,@l,@s,@y,@t,@a)",new("@n",name.Trim()),new("@d",department.Trim()),new("@p",program.Trim()),new("@l",studyLevel.Trim()),new("@s",semester),new("@y",year.Trim()),new("@t",(object?)homeTeacher??DBNull.Value),new("@a",active))
            :db.ExecuteAsync("UPDATE Rooms SET RoomName=@n,Department=@d,ProgramName=@p,StudyLevel=@l,Semester=@s,AcademicYear=@y,HomeRoomTeacherId=@t,IsActive=@a WHERE RoomId=@id",new("@n",name.Trim()),new("@d",department.Trim()),new("@p",program.Trim()),new("@l",studyLevel.Trim()),new("@s",semester),new("@y",year.Trim()),new("@t",(object?)homeTeacher??DBNull.Value),new("@a",active),new("@id",id));
    }
    public Task SaveSubjectAsync(int? id,string name,string code,bool active)=>id is null
        ?db.ExecuteAsync("INSERT Subjects(SubjectName,SubjectCode,IsActive) VALUES(@n,@c,@a)",new("@n",name),new("@c",code),new("@a",active))
        :db.ExecuteAsync("UPDATE Subjects SET SubjectName=@n,SubjectCode=@c,IsActive=@a WHERE SubjectId=@id",new("@n",name),new("@c",code),new("@a",active),new("@id",id));
    public Task SaveAssignmentAsync(int teacherId,int roomId,int subjectId)=>db.ExecuteAsync("MERGE TeacherAssignments AS t USING(SELECT @t TeacherId,r.RoomId,@s SubjectId,r.AcademicYear FROM Rooms r WHERE r.RoomId=@r) s ON t.TeacherId=s.TeacherId AND t.RoomId=s.RoomId AND t.SubjectId=s.SubjectId AND t.AcademicYear=s.AcademicYear WHEN MATCHED THEN UPDATE SET IsActive=1 WHEN NOT MATCHED THEN INSERT(TeacherId,RoomId,SubjectId,AcademicYear) VALUES(s.TeacherId,s.RoomId,s.SubjectId,s.AcademicYear);",new("@t",teacherId),new("@r",roomId),new("@s",subjectId));
    public async Task SaveTimetableAsync(int teacherId,int roomId,int subjectId,int dayOfWeek,TimeSpan start,TimeSpan end,DateTime effectiveFrom,DateTime effectiveTo)
    {
        if(dayOfWeek is <1 or >7||end<=start||effectiveTo.Date<effectiveFrom.Date)throw new ArgumentException("Enter a valid weekday, time range and effective date range.");
        const string sql="IF NOT EXISTS(SELECT 1 FROM TeacherAssignments ta JOIN Rooms r ON r.RoomId=ta.RoomId WHERE ta.TeacherId=@t AND ta.RoomId=@r AND ta.SubjectId=@s AND ta.IsActive=1 AND r.IsActive=1) THROW 51010,'Create an active lecturer assignment for this class and course first.',1; INSERT Timetables(RoomId,SubjectId,TeacherId,DayOfWeek,StartTime,EndTime,AcademicYear,Semester,EffectiveFrom,EffectiveTo) SELECT RoomId,@s,@t,@d,@start,@end,AcademicYear,Semester,@from,@to FROM Rooms WHERE RoomId=@r;";
        await db.ExecuteAsync(sql,new("@t",teacherId),new("@r",roomId),new("@s",subjectId),new("@d",dayOfWeek),new("@start",start),new("@end",end),new("@from",effectiveFrom.Date),new("@to",effectiveTo.Date));
    }
    public Task SetTimetableActiveAsync(int timetableId,bool active)=>db.ExecuteAsync("UPDATE Timetables SET IsActive=@a WHERE TimetableId=@id",new("@a",active),new("@id",timetableId));
}
