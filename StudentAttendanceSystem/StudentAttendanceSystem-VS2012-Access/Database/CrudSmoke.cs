using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
namespace StudentAttendanceSystemVS2012 {
 static class CrudSmoke {
  static int Id(Database db,string table,string key){return Convert.ToInt32(db.Query("SELECT MAX(["+key+"]) AS [Value] FROM ["+table+"]").Rows[0][0]);}
  static void Main(string[] a){
   var db=new Database(a[0]);
   db.Execute("INSERT Users(Username,PasswordHash,FullName,Role,IsActive,CreatedAt) VALUES(@u,@p,@n,'Teacher',True,Now())",new SqlParameter("@u","testteacher"),new SqlParameter("@p","hash"),new SqlParameter("@n","Test Teacher"));
   int uid=Convert.ToInt32(db.Query("SELECT UserId FROM Users WHERE Username=@u",new SqlParameter("@u","testteacher")).Rows[0][0]);
   db.Execute("INSERT Teachers(UserId,TeacherCode,FullName,Phone) VALUES(@u,@c,@n,@p)",new SqlParameter("@u",uid),new SqlParameter("@c","T-001"),new SqlParameter("@n","Test Teacher"),new SqlParameter("@p","091"));
   int teacher=Id(db,"Teachers","TeacherId");
   db.Execute("INSERT Rooms(RoomName,Department,ProgramName,StudyLevel,Semester,AcademicYear,HomeRoomTeacherId,IsActive) VALUES(@r,@d,@p,@l,@s,@y,@t,True)",new SqlParameter("@r","CS-3A"),new SqlParameter("@d","CS"),new SqlParameter("@p","B.C.Sc"),new SqlParameter("@l","Third Year"),new SqlParameter("@s",6),new SqlParameter("@y","2025-2026"),new SqlParameter("@t",teacher));
   int room=Id(db,"Rooms","RoomId");
   db.Execute("INSERT Subjects(SubjectCode,SubjectName,CourseYear,IsActive) VALUES(@c,@n,@y,True)",new SqlParameter("@c","CST-3242"),new SqlParameter("@n","Probability and Statistics"),new SqlParameter("@y",3));
   int subject=Id(db,"Subjects","SubjectId");
   db.Execute("INSERT TeacherAssignments(TeacherId,RoomId,SubjectId,AcademicYear,IsActive) VALUES(@t,@r,@s,@y,True)",new SqlParameter("@t",teacher),new SqlParameter("@r",room),new SqlParameter("@s",subject),new SqlParameter("@y","2025-2026"));
   db.Execute("INSERT Timetables(RoomId,SubjectId,TeacherId,DayOfWeek,StartTime,EndTime,AcademicYear,Semester,EffectiveFrom,EffectiveTo,IsActive) VALUES(@r,@s,@t,@d,@st,@et,@y,@sem,@f,@to,True)",new SqlParameter("@r",room),new SqlParameter("@s",subject),new SqlParameter("@t",teacher),new SqlParameter("@d",1),new SqlParameter("@st",TimeSpan.FromHours(9)),new SqlParameter("@et",TimeSpan.FromHours(10)),new SqlParameter("@y","2025-2026"),new SqlParameter("@sem",6),new SqlParameter("@f",new DateTime(2026,7,1)),new SqlParameter("@to",new DateTime(2026,12,31)));
   int timetable=Id(db,"Timetables","TimetableId");
   db.Execute("INSERT Students(StudentCode,StudentName,Gender,RoomId,AcademicYear,IsActive) VALUES(@c,@n,@g,@r,@y,True)",new SqlParameter("@c","S-001"),new SqlParameter("@n","Test Student"),new SqlParameter("@g","Other"),new SqlParameter("@r",room),new SqlParameter("@y","2025-2026"));
   int student=Id(db,"Students","StudentId");
   db.Execute("INSERT ClassSessions(TimetableId,RoomId,SubjectId,TeacherId,SessionDate) VALUES(@tt,@r,@s,@t,@d)",new SqlParameter("@tt",timetable),new SqlParameter("@r",room),new SqlParameter("@s",subject),new SqlParameter("@t",teacher),new SqlParameter("@d",new DateTime(2026,9,14)));
   int session=Id(db,"ClassSessions","SessionId");
   db.Execute("MERGE Attendance",new SqlParameter("@session",session),new SqlParameter("@student",student),new SqlParameter("@r",room),new SqlParameter("@s",subject),new SqlParameter("@t",teacher),new SqlParameter("@d",new DateTime(2026,9,14)),new SqlParameter("@status","Present"),new SqlParameter("@remark","OK"));
   Console.WriteLine("CRUD_OK Users={0} Teachers={1} Rooms={2} Subjects={3} Assignments={4} Timetables={5} Students={6} Sessions={7} Attendance={8}",db.Query("SELECT COUNT(*) AS [Value] FROM Users").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM Teachers").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM Rooms").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM Subjects").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM TeacherAssignments").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM Timetables").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM Students").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM ClassSessions").Rows[0][0],db.Query("SELECT COUNT(*) AS [Value] FROM Attendance").Rows[0][0]);
  }
 }
}
