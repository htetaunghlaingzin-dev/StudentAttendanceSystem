using Microsoft.Data.SqlClient;
using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Models;

namespace StudentAttendanceSystem.Services;

public sealed class AttendanceService(Database db,AuthorizationService authorization)
{
    public async Task<List<ScheduledAttendanceSession>> GetScheduledSessionsAsync(UserSession user,DateTime date)
    {
        if(user.Role!=UserRole.Teacher||user.TeacherId is null)return [];
        var day=date.DayOfWeek==DayOfWeek.Sunday?7:(int)date.DayOfWeek;
        const string create="INSERT ClassSessions(TimetableId,RoomId,SubjectId,TeacherId,SessionDate) SELECT tt.TimetableId,tt.RoomId,tt.SubjectId,tt.TeacherId,@date FROM Timetables tt JOIN Rooms r ON r.RoomId=tt.RoomId JOIN Subjects s ON s.SubjectId=tt.SubjectId JOIN Users u ON u.UserId=(SELECT UserId FROM Teachers WHERE TeacherId=tt.TeacherId) WHERE tt.TeacherId=@teacher AND tt.DayOfWeek=@day AND @date BETWEEN tt.EffectiveFrom AND tt.EffectiveTo AND tt.IsActive=1 AND r.IsActive=1 AND s.IsActive=1 AND u.IsActive=1 AND NOT EXISTS(SELECT 1 FROM ClassSessions cs WHERE cs.TimetableId=tt.TimetableId AND cs.SessionDate=@date);";
        await db.ExecuteAsync(create,new("@date",date.Date),new("@teacher",user.TeacherId.Value),new("@day",day));
        const string query="SELECT cs.SessionId,cs.RoomId,cs.SubjectId,r.ProgramName+' | '+r.RoomName+' | '+r.StudyLevel+' | Sem '+CAST(r.Semester AS varchar(2))+' | '+r.AcademicYear,s.SubjectCode+' - '+s.SubjectName,tt.StartTime,tt.EndTime,cs.SessionStatus FROM ClassSessions cs JOIN Timetables tt ON tt.TimetableId=cs.TimetableId JOIN Rooms r ON r.RoomId=cs.RoomId JOIN Subjects s ON s.SubjectId=cs.SubjectId WHERE cs.TeacherId=@teacher AND cs.SessionDate=@date AND tt.IsActive=1 ORDER BY tt.StartTime";
        return await db.QueryAsync<ScheduledAttendanceSession>(query,r=>new(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetString(3),r.GetString(4),r.GetTimeSpan(5),r.GetTimeSpan(6),r.GetString(7)),new SqlParameter("@teacher",user.TeacherId.Value),new SqlParameter("@date",date.Date));
    }

    public async Task<List<AttendanceRow>> GetSessionRowsAsync(UserSession user,int sessionId)
    {
        var session=await GetOwnedSessionAsync(user,sessionId);
        return await db.QueryAsync<AttendanceRow>("SELECT st.StudentId,st.StudentCode,st.StudentName,COALESCE(a.Status,'Present'),a.Remark FROM Students st LEFT JOIN Attendance a ON a.SessionId=@session AND a.StudentId=st.StudentId WHERE st.RoomId=@room AND st.IsActive=1 ORDER BY st.StudentId",r=>new(r.GetInt32(0),r.GetString(1),r.GetString(2),Enum.Parse<AttendanceStatus>(r.GetString(3)),r.IsDBNull(4)?null:r.GetString(4)),new SqlParameter("@session",sessionId),new SqlParameter("@room",session.RoomId));
    }

    public async Task SaveSessionAsync(UserSession user,int sessionId,IEnumerable<AttendanceRow> rows)
    {
        var session=await GetOwnedSessionAsync(user,sessionId);
        if(session.Status=="Cancelled")throw new InvalidOperationException("Attendance cannot be recorded for a cancelled session.");
        await authorization.EnsureCanTakeAttendanceAsync(user,session.RoomId,session.SubjectId);
        await using var c=db.CreateConnection();await c.OpenAsync();await using var tx=await c.BeginTransactionAsync();
        try{foreach(var row in rows){if(row.Status is not (AttendanceStatus.Present or AttendanceStatus.Absent))throw new InvalidOperationException("New attendance must be Present or Absent.");var cmd=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM Students WHERE StudentId=@student AND RoomId=@room AND IsActive=1) THROW 51011,'Student is not active in this class.',1; MERGE Attendance t USING(SELECT @session SessionId,@student StudentId) s ON t.SessionId=s.SessionId AND t.StudentId=s.StudentId WHEN MATCHED THEN UPDATE SET Status=@status,Remark=@remark,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@teacher WHEN NOT MATCHED THEN INSERT(SessionId,StudentId,RoomId,SubjectId,TeacherId,AttendanceDate,Status,Remark,UpdatedBy) VALUES(@session,@student,@room,@subject,@teacher,@date,@status,@remark,@teacher);",c,(SqlTransaction)tx);cmd.Parameters.AddRange([new("@session",sessionId),new("@student",row.StudentId),new("@room",session.RoomId),new("@subject",session.SubjectId),new("@teacher",user.TeacherId!.Value),new("@date",session.Date),new("@status",row.Status.ToString()),new("@remark",(object?)row.Remark??DBNull.Value)]);await cmd.ExecuteNonQueryAsync();}var update=new SqlCommand("UPDATE ClassSessions SET SessionStatus='Held',UpdatedAt=SYSUTCDATETIME() WHERE SessionId=@id",c,(SqlTransaction)tx);update.Parameters.AddWithValue("@id",sessionId);await update.ExecuteNonQueryAsync();await tx.CommitAsync();}catch{await tx.RollbackAsync();throw;}
    }

    public async Task SetSessionStatusAsync(UserSession user,int sessionId,string status)
    {
        if(status is not ("Cancelled" or "Rescheduled" or "Scheduled"))throw new ArgumentException("Invalid session status.");
        await GetOwnedSessionAsync(user,sessionId);
        if(await db.ExecuteAsync("UPDATE ClassSessions SET SessionStatus=@s,UpdatedAt=SYSUTCDATETIME() WHERE SessionId=@id AND NOT EXISTS(SELECT 1 FROM Attendance WHERE SessionId=@id)",new("@s",status),new("@id",sessionId))==0)throw new InvalidOperationException("A session with attendance records cannot be cancelled or rescheduled.");
    }

    async Task<(int RoomId,int SubjectId,DateTime Date,string Status)> GetOwnedSessionAsync(UserSession user,int sessionId)
    {
        if(user.TeacherId is null)throw new UnauthorizedAccessException();
        var sessions=await db.QueryAsync("SELECT RoomId,SubjectId,SessionDate,SessionStatus FROM ClassSessions WHERE SessionId=@id AND TeacherId=@teacher",r=>(r.GetInt32(0),r.GetInt32(1),r.GetDateTime(2),r.GetString(3)),new SqlParameter("@id",sessionId),new SqlParameter("@teacher",user.TeacherId.Value));
        return sessions.SingleOrDefault() is var x&&x.Item1!=0?x:throw new UnauthorizedAccessException("This session is not assigned to the logged-in lecturer.");
    }
    public Task<List<AttendanceAssignment>> GetAttendanceAssignmentsAsync(UserSession s)
    {
        if(s.Role!=UserRole.Teacher||s.TeacherId is null)return Task.FromResult(new List<AttendanceAssignment>());
        return db.QueryAsync<AttendanceAssignment>("SELECT r.RoomId,sub.SubjectId,r.ProgramName+' | '+r.RoomName+' | '+r.StudyLevel+' | Sem '+CAST(r.Semester AS varchar(2))+' | '+r.AcademicYear,sub.SubjectCode+' - '+sub.SubjectName FROM TeacherAssignments ta JOIN Rooms r ON r.RoomId=ta.RoomId JOIN Subjects sub ON sub.SubjectId=ta.SubjectId JOIN Teachers t ON t.TeacherId=ta.TeacherId JOIN Users u ON u.UserId=t.UserId WHERE ta.TeacherId=@t AND ta.IsActive=1 AND r.IsActive=1 AND sub.IsActive=1 AND u.IsActive=1 ORDER BY r.ProgramName,r.RoomName,sub.SubjectName",r=>new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3)),new SqlParameter("@t",s.TeacherId.Value));
    }
    public async Task<List<LookupItem>> GetAccessibleRoomsAsync(UserSession s)
    {
        if(s.Role==UserRole.Admin) return await db.QueryAsync<LookupItem>("SELECT RoomId,ProgramName+' | '+RoomName+' | '+StudyLevel+' | Sem '+CAST(Semester AS varchar(1))+' | '+AcademicYear FROM Rooms WHERE IsActive=1 ORDER BY ProgramName,StudyLevel,RoomName",r=>new(r.GetInt32(0),r.GetString(1)));
        return await db.QueryAsync<LookupItem>("SELECT DISTINCT r.RoomId,r.ProgramName+' | '+r.RoomName+' | '+r.StudyLevel+' | Sem '+CAST(r.Semester AS varchar(1))+' | '+r.AcademicYear FROM Rooms r LEFT JOIN TeacherAssignments ta ON ta.RoomId=r.RoomId AND ta.TeacherId=@t AND ta.IsActive=1 WHERE r.IsActive=1 AND (ta.AssignmentId IS NOT NULL OR r.HomeRoomTeacherId=@t) ORDER BY 2",r=>new(r.GetInt32(0),r.GetString(1)),new SqlParameter("@t",s.TeacherId));
    }
    public Task<List<LookupItem>> GetHomeRoomsAsync(UserSession s)
    {
        if(s.Role!=UserRole.Teacher||s.TeacherId is null)return Task.FromResult(new List<LookupItem>());
        return db.QueryAsync<LookupItem>("SELECT RoomId,ProgramName+' | '+RoomName+' | '+StudyLevel+' | Sem '+CAST(Semester AS varchar(2))+' | '+AcademicYear FROM Rooms WHERE HomeRoomTeacherId=@teacher AND IsActive=1 ORDER BY ProgramName,RoomName",r=>new(r.GetInt32(0),r.GetString(1)),new SqlParameter("@teacher",s.TeacherId.Value));
    }
    public async Task<List<LookupItem>> GetAccessibleSubjectsAsync(UserSession s,int roomId,bool includeHomeRoomReadOnly)
    {
        if(s.Role==UserRole.Admin || (includeHomeRoomReadOnly && s.TeacherId.HasValue && await authorization.IsHomeRoomTeacherAsync(s.TeacherId.Value,roomId)))
            return await db.QueryAsync<LookupItem>("SELECT DISTINCT s.SubjectId,s.SubjectCode+' - '+s.SubjectName FROM Subjects s JOIN TeacherAssignments ta ON ta.SubjectId=s.SubjectId WHERE ta.RoomId=@r AND ta.IsActive=1 AND s.IsActive=1 ORDER BY 2",r=>new(r.GetInt32(0),r.GetString(1)),new SqlParameter("@r",roomId));
        return await db.QueryAsync<LookupItem>("SELECT s.SubjectId,s.SubjectCode+' - '+s.SubjectName FROM Subjects s JOIN TeacherAssignments ta ON ta.SubjectId=s.SubjectId WHERE ta.RoomId=@r AND ta.TeacherId=@t AND ta.IsActive=1 AND s.IsActive=1 ORDER BY 2",r=>new(r.GetInt32(0),r.GetString(1)),new SqlParameter("@r",roomId),new SqlParameter("@t",s.TeacherId));
    }
    public async Task<List<AttendanceRow>> GetEntryRowsAsync(UserSession s,int roomId,int subjectId,DateTime date)
    {
        if(s.Role!=UserRole.Admin && (s.TeacherId is null || !await authorization.CanViewAttendanceAsync(s.TeacherId.Value,roomId,subjectId))) throw new UnauthorizedAccessException("You cannot view this attendance.");
        return await db.QueryAsync<AttendanceRow>("SELECT st.StudentId,st.StudentCode,st.StudentName,COALESCE(a.Status,'Present'),a.Remark FROM Students st LEFT JOIN Attendance a ON a.StudentId=st.StudentId AND a.SubjectId=@s AND a.AttendanceDate=@d WHERE st.RoomId=@r AND st.IsActive=1 ORDER BY st.StudentId",r=>new(r.GetInt32(0),r.GetString(1),r.GetString(2),Enum.Parse<AttendanceStatus>(r.GetString(3)),r.IsDBNull(4)?null:r.GetString(4)),new("@s",subjectId),new("@d",date.Date),new("@r",roomId));
    }
    public async Task SaveAsync(UserSession s,int roomId,int subjectId,DateTime date,IEnumerable<AttendanceRow> rows)
    {
        await authorization.EnsureCanTakeAttendanceAsync(s,roomId,subjectId);
        var teacherId=s.TeacherId??throw new InvalidOperationException("Admin must select/impersonate the assigned teacher to record attendance.");
        await using var c=db.CreateConnection(); await c.OpenAsync(); await using var tx=await c.BeginTransactionAsync();
        try { foreach(var row in rows) {
            var cmd=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM Students WHERE StudentId=@st AND RoomId=@r AND IsActive=1) THROW 51000,'Student is not active in this room.',1; IF EXISTS(SELECT 1 FROM Attendance WHERE StudentId=@st AND SubjectId=@s AND AttendanceDate=@d AND TeacherId<>@teacher) THROW 51001,'Attendance was recorded by another teacher and cannot be changed.',1; MERGE Attendance AS t USING(SELECT @st StudentId,@r RoomId,@s SubjectId,@d AttendanceDate) x ON t.StudentId=x.StudentId AND t.SubjectId=x.SubjectId AND t.AttendanceDate=x.AttendanceDate WHEN MATCHED AND t.TeacherId=@teacher THEN UPDATE SET Status=@status,Remark=@remark,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@teacher WHEN NOT MATCHED THEN INSERT(StudentId,RoomId,SubjectId,TeacherId,AttendanceDate,Status,Remark,UpdatedBy) VALUES(@st,@r,@s,@teacher,@d,@status,@remark,@teacher);",c,(SqlTransaction)tx);
            cmd.Parameters.AddRange([new("@st",row.StudentId),new("@r",roomId),new("@s",subjectId),new("@d",date.Date),new("@teacher",teacherId),new("@status",row.Status.ToString()),new("@remark",(object?)row.Remark??DBNull.Value)]); await cmd.ExecuteNonQueryAsync(); }
            await tx.CommitAsync();
        } catch { await tx.RollbackAsync(); throw; }
    }
    public async Task<List<MonthlyReportRow>> GetMonthlyReportAsync(UserSession s,int roomId,int month,int year)
    {
        if(s.Role!=UserRole.Admin && (s.TeacherId is null || !await authorization.CanManageStudentsAsync(s.TeacherId.Value,roomId))) throw new UnauthorizedAccessException("You cannot view this room report.");
        await db.ExecuteAsync("EXEC RecalculateMonthlyAttendance @RoomId,@Month,@Year",new("@RoomId",roomId),new("@Month",month),new("@Year",year));
        return await db.QueryAsync<MonthlyReportRow>("SELECT st.StudentCode,st.StudentName,r.RoomName,m.TotalSessions,m.PresentCount,m.AbsentCount,m.LateCount,m.ExcusedCount,m.AttendancePercentage,m.Remark FROM MonthlyAttendanceSummary m JOIN Students st ON st.StudentId=m.StudentId JOIN Rooms r ON r.RoomId=m.RoomId WHERE m.RoomId=@r AND m.[Month]=@m AND m.[Year]=@y ORDER BY st.StudentId",r=>new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt32(3),r.GetInt32(4),r.GetInt32(5),r.GetInt32(6),r.GetInt32(7),r.GetDecimal(8),r.GetString(9)),new("@r",roomId),new("@m",month),new("@y",year));
    }
}
