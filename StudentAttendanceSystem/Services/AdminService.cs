using Microsoft.Data.SqlClient;
using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Helpers;
using StudentAttendanceSystem.Models;

namespace StudentAttendanceSystem.Services;

public sealed class AdminService(Database db)
{
    public Task<List<LookupItem>> GetRoomsAsync(bool activeOnly=true)=>db.QueryAsync<LookupItem>($"SELECT RoomId,ProgramName+' | '+RoomName+' | '+StudyLevel+' | Sem '+CAST(Semester AS varchar(1))+' | '+AcademicYear FROM Rooms{(activeOnly?" WHERE IsActive=1":"")} ORDER BY RoomId",r=>new(r.GetInt32(0),r.GetString(1)));
    public Task<List<LookupItem>> GetSubjectsAsync(bool activeOnly=true,int? courseYear=null)=>db.QueryAsync<LookupItem>($"SELECT SubjectId,SubjectCode+' - '+SubjectName FROM Subjects WHERE 1=1{(activeOnly?" AND IsActive=1":"")}{(courseYear.HasValue?" AND CourseYear=@year":"")} ORDER BY CourseYear,SubjectCode",r=>new(r.GetInt32(0),r.GetString(1)),courseYear.HasValue?new SqlParameter("@year",courseYear.Value):new SqlParameter("@year",DBNull.Value));
    public Task<List<LookupItem>> GetSubjectsForRoomAsync(int roomId,bool activeOnly=true)=>db.QueryAsync<LookupItem>($"SELECT s.SubjectId,s.SubjectCode+' - '+s.SubjectName FROM Subjects s JOIN Rooms r ON r.RoomId=@room WHERE s.CourseYear=CASE WHEN LOWER(r.StudyLevel) LIKE '%fifth%' OR r.StudyLevel LIKE '%5%' THEN 5 WHEN LOWER(r.StudyLevel) LIKE '%fourth%' OR r.StudyLevel LIKE '%4%' THEN 4 WHEN LOWER(r.StudyLevel) LIKE '%third%' OR r.StudyLevel LIKE '%3%' THEN 3 WHEN LOWER(r.StudyLevel) LIKE '%second%' OR r.StudyLevel LIKE '%2%' THEN 2 ELSE 1 END{(activeOnly?" AND s.IsActive=1":"")} ORDER BY s.SubjectCode",r=>new(r.GetInt32(0),r.GetString(1)),new SqlParameter("@room",roomId));
    public Task<List<LookupItem>> GetTeachersAsync(bool activeOnly=true)=>db.QueryAsync<LookupItem>($"SELECT t.TeacherId,t.TeacherCode+' - '+t.FullName FROM Teachers t JOIN Users u ON u.UserId=t.UserId{(activeOnly?" WHERE u.IsActive=1":"")} ORDER BY t.TeacherId",r=>new(r.GetInt32(0),r.GetString(1)));
    public Task<List<LookupItem>> GetAssignedTeachersAsync(int roomId,int subjectId)=>db.QueryAsync<LookupItem>("SELECT DISTINCT t.TeacherId,t.TeacherCode+' - '+t.FullName FROM TeacherAssignments ta JOIN Teachers t ON t.TeacherId=ta.TeacherId JOIN Users u ON u.UserId=t.UserId WHERE ta.RoomId=@r AND ta.SubjectId=@s AND ta.IsActive=1 AND u.IsActive=1 ORDER BY t.TeacherCode+' - '+t.FullName",r=>new(r.GetInt32(0),r.GetString(1)),new("@r",roomId),new("@s",subjectId));

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
    public Task SetRoomActiveAsync(int roomId,bool active)=>db.ExecuteAsync("UPDATE Rooms SET IsActive=@a WHERE RoomId=@id",new("@a",active),new("@id",roomId));
    public Task SetSubjectActiveAsync(int subjectId,bool active)=>db.ExecuteAsync("UPDATE Subjects SET IsActive=@a WHERE SubjectId=@id",new("@a",active),new("@id",subjectId));
    public Task SetAssignmentActiveAsync(int assignmentId,bool active)=>db.ExecuteAsync("UPDATE TeacherAssignments SET IsActive=@a WHERE AssignmentId=@id",new("@a",active),new("@id",assignmentId));
    public Task SaveRoomAsync(int? id,string name,string department,string program,string studyLevel,int semester,string year,int? homeTeacher,bool active)
    {
        if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(department)||string.IsNullOrWhiteSpace(program)||string.IsNullOrWhiteSpace(studyLevel)||string.IsNullOrWhiteSpace(year)) throw new ArgumentException("Class/section, department, program, study level and academic year are required.");
        if(semester is <1 or >10) throw new ArgumentException("Semester must be between 1 and 10.");
        return id is null
            ?db.ExecuteAsync("INSERT Rooms(RoomName,Department,ProgramName,StudyLevel,Semester,AcademicYear,HomeRoomTeacherId,IsActive) VALUES(@n,@d,@p,@l,@s,@y,@t,@a)",new("@n",name.Trim()),new("@d",department.Trim()),new("@p",program.Trim()),new("@l",studyLevel.Trim()),new("@s",semester),new("@y",year.Trim()),new("@t",(object?)homeTeacher??DBNull.Value),new("@a",active))
            :db.ExecuteAsync("UPDATE Rooms SET RoomName=@n,Department=@d,ProgramName=@p,StudyLevel=@l,Semester=@s,AcademicYear=@y,HomeRoomTeacherId=@t,IsActive=@a WHERE RoomId=@id",new("@n",name.Trim()),new("@d",department.Trim()),new("@p",program.Trim()),new("@l",studyLevel.Trim()),new("@s",semester),new("@y",year.Trim()),new("@t",(object?)homeTeacher??DBNull.Value),new("@a",active),new("@id",id));
    }
    public Task SaveSubjectAsync(int? id,string name,string code,int courseYear,bool active)
    {
        if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(code))throw new ArgumentException("Course code and name are required.");
        if(courseYear is <1 or >5)throw new ArgumentException("Course year must be between 1 and 5.");
        return id is null?db.ExecuteAsync("INSERT Subjects(SubjectName,SubjectCode,CourseYear,IsActive) VALUES(@n,@c,@y,@a)",new("@n",name.Trim()),new("@c",code.Trim()),new("@y",courseYear),new("@a",active)):db.ExecuteAsync("UPDATE Subjects SET SubjectName=@n,SubjectCode=@c,CourseYear=@y,IsActive=@a WHERE SubjectId=@id",new("@n",name.Trim()),new("@c",code.Trim()),new("@y",courseYear),new("@a",active),new("@id",id));
    }
    public Task SaveAssignmentAsync(int teacherId,int roomId,int subjectId,int? assignmentId=null)=>db.ExecuteAsync("DECLARE @roomYear tinyint=(SELECT CASE WHEN LOWER(StudyLevel) LIKE '%fifth%' OR StudyLevel LIKE '%5%' THEN 5 WHEN LOWER(StudyLevel) LIKE '%fourth%' OR StudyLevel LIKE '%4%' THEN 4 WHEN LOWER(StudyLevel) LIKE '%third%' OR StudyLevel LIKE '%3%' THEN 3 WHEN LOWER(StudyLevel) LIKE '%second%' OR StudyLevel LIKE '%2%' THEN 2 ELSE 1 END FROM Rooms WHERE RoomId=@r); IF NOT EXISTS(SELECT 1 FROM Subjects WHERE SubjectId=@s AND CourseYear=@roomYear) THROW 51013,'Choose a course for the selected class year.',1; IF EXISTS(SELECT 1 FROM TeacherAssignments ta JOIN Rooms rm ON rm.RoomId=@r WHERE ta.RoomId=@r AND ta.SubjectId=@s AND ta.AcademicYear=rm.AcademicYear AND ta.AssignmentId<>ISNULL(@id,-1)) THROW 51014,'This course has already been assigned to the selected class.',1; IF @id IS NULL INSERT TeacherAssignments(TeacherId,RoomId,SubjectId,AcademicYear,IsActive) SELECT @t,@r,@s,AcademicYear,1 FROM Rooms WHERE RoomId=@r; ELSE UPDATE ta SET TeacherId=@t,RoomId=@r,SubjectId=@s,AcademicYear=rm.AcademicYear FROM TeacherAssignments ta JOIN Rooms rm ON rm.RoomId=@r WHERE ta.AssignmentId=@id;",new("@t",teacherId),new("@r",roomId),new("@s",subjectId),new("@id",(object?)assignmentId??DBNull.Value));
    public async Task SaveTimetableAsync(int teacherId,int roomId,int subjectId,int dayOfWeek,TimeSpan start,TimeSpan end,DateTime effectiveFrom,DateTime effectiveTo,int? timetableId=null)
    {
        if(dayOfWeek is <1 or >7||end<=start||effectiveTo.Date<effectiveFrom.Date)throw new ArgumentException("Enter a valid weekday, time range and effective date range.");
        const string sql="IF NOT EXISTS(SELECT 1 FROM Rooms WHERE RoomId=@r AND IsActive=1) THROW 51010,'Select an active class.',1; DECLARE @roomYear tinyint=(SELECT CASE WHEN LOWER(StudyLevel) LIKE '%fifth%' OR StudyLevel LIKE '%5%' THEN 5 WHEN LOWER(StudyLevel) LIKE '%fourth%' OR StudyLevel LIKE '%4%' THEN 4 WHEN LOWER(StudyLevel) LIKE '%third%' OR StudyLevel LIKE '%3%' THEN 3 WHEN LOWER(StudyLevel) LIKE '%second%' OR StudyLevel LIKE '%2%' THEN 2 ELSE 1 END FROM Rooms WHERE RoomId=@r); IF NOT EXISTS(SELECT 1 FROM Subjects WHERE SubjectId=@s AND CourseYear=@roomYear) THROW 51013,'Choose a course for the selected class year.',1; IF NOT EXISTS(SELECT 1 FROM TeacherAssignments ta JOIN Users u ON u.UserId=(SELECT UserId FROM Teachers WHERE TeacherId=ta.TeacherId) WHERE ta.TeacherId=@t AND ta.RoomId=@r AND ta.SubjectId=@s AND ta.IsActive=1 AND u.IsActive=1) THROW 51011,'The selected lecturer is not actively assigned to this class and course.',1; DECLARE @year nvarchar(20),@semester tinyint; SELECT @year=AcademicYear,@semester=Semester FROM Rooms WHERE RoomId=@r; IF EXISTS(SELECT 1 FROM Timetables tt WHERE tt.TimetableId<>ISNULL(@id,-1) AND tt.RoomId=@r AND tt.SubjectId=@s AND tt.TeacherId=@t AND tt.DayOfWeek=@d AND tt.StartTime=@start AND tt.AcademicYear=@year AND tt.Semester=@semester) THROW 51015,'This timetable session has already been chosen.',1; IF EXISTS(SELECT 1 FROM Timetables tt WHERE tt.IsActive=1 AND tt.TimetableId<>ISNULL(@id,-1) AND tt.DayOfWeek=@d AND tt.AcademicYear=@year AND tt.Semester=@semester AND (tt.RoomId=@r OR tt.TeacherId=@t) AND @start<tt.EndTime AND @end>tt.StartTime AND @from<=tt.EffectiveTo AND @to>=tt.EffectiveFrom) THROW 51012,'This time is already taken by the selected class or lecturer.',1; IF @id IS NULL INSERT Timetables(RoomId,SubjectId,TeacherId,DayOfWeek,StartTime,EndTime,AcademicYear,Semester,EffectiveFrom,EffectiveTo,IsActive) VALUES(@r,@s,@t,@d,@start,@end,@year,@semester,@from,@to,1); ELSE UPDATE Timetables SET RoomId=@r,SubjectId=@s,TeacherId=@t,DayOfWeek=@d,StartTime=@start,EndTime=@end,AcademicYear=@year,Semester=@semester,EffectiveFrom=@from,EffectiveTo=@to WHERE TimetableId=@id;";
        await db.ExecuteAsync(sql,new("@t",teacherId),new("@r",roomId),new("@s",subjectId),new("@d",dayOfWeek),new("@start",start),new("@end",end),new("@from",effectiveFrom.Date),new("@to",effectiveTo.Date),new("@id",(object?)timetableId??DBNull.Value));
    }
    public Task SetTimetableActiveAsync(int timetableId,bool active)=>db.ExecuteAsync("UPDATE Timetables SET IsActive=@a WHERE TimetableId=@id",new("@a",active),new("@id",timetableId));
}
