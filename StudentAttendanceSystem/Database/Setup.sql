SET NOCOUNT ON;
IF DB_ID(N'StudentAttendanceSystem') IS NULL CREATE DATABASE StudentAttendanceSystem;
GO
USE StudentAttendanceSystem;
GO
CREATE TABLE Users(UserId int IDENTITY PRIMARY KEY,Username nvarchar(50) NOT NULL UNIQUE,PasswordHash nvarchar(300) NOT NULL,FullName nvarchar(120) NOT NULL,Role varchar(10) NOT NULL CHECK(Role IN('Admin','Teacher')),IsActive bit NOT NULL DEFAULT 1,CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE Teachers(TeacherId int IDENTITY PRIMARY KEY,UserId int NOT NULL UNIQUE REFERENCES Users(UserId),TeacherCode nvarchar(30) NOT NULL UNIQUE,FullName nvarchar(120) NOT NULL,Phone nvarchar(30) NULL);
CREATE TABLE Rooms(RoomId int IDENTITY PRIMARY KEY,RoomName nvarchar(80) NOT NULL,Department nvarchar(100) NOT NULL,ProgramName nvarchar(120) NOT NULL,StudyLevel nvarchar(30) NOT NULL,Semester tinyint NOT NULL CONSTRAINT CK_Rooms_Semester CHECK(Semester BETWEEN 1 AND 10),AcademicYear nvarchar(20) NOT NULL,HomeRoomTeacherId int NULL REFERENCES Teachers(TeacherId),IsActive bit NOT NULL DEFAULT 1,CONSTRAINT UQ_Room UNIQUE(ProgramName,RoomName,AcademicYear,Semester));
CREATE TABLE Subjects(SubjectId int IDENTITY PRIMARY KEY,SubjectName nvarchar(80) NOT NULL,SubjectCode nvarchar(20) NOT NULL UNIQUE,IsActive bit NOT NULL DEFAULT 1);
CREATE TABLE TeacherAssignments(AssignmentId int IDENTITY PRIMARY KEY,TeacherId int NOT NULL REFERENCES Teachers(TeacherId),RoomId int NOT NULL REFERENCES Rooms(RoomId),SubjectId int NOT NULL REFERENCES Subjects(SubjectId),AcademicYear nvarchar(20) NOT NULL,IsActive bit NOT NULL DEFAULT 1,CONSTRAINT UQ_Assignment UNIQUE(TeacherId,RoomId,SubjectId,AcademicYear));
CREATE TABLE Students(StudentId int IDENTITY PRIMARY KEY,StudentCode nvarchar(30) NOT NULL,StudentName nvarchar(120) NOT NULL,Gender nvarchar(15) NULL,RoomId int NOT NULL REFERENCES Rooms(RoomId),AcademicYear nvarchar(20) NOT NULL,IsActive bit NOT NULL DEFAULT 1,CONSTRAINT UQ_StudentCodeYear UNIQUE(StudentCode,AcademicYear));
CREATE TABLE Timetables(TimetableId int IDENTITY PRIMARY KEY,RoomId int NOT NULL REFERENCES Rooms(RoomId),SubjectId int NOT NULL REFERENCES Subjects(SubjectId),TeacherId int NOT NULL REFERENCES Teachers(TeacherId),DayOfWeek tinyint NOT NULL CHECK(DayOfWeek BETWEEN 1 AND 7),StartTime time(0) NOT NULL,EndTime time(0) NOT NULL,AcademicYear nvarchar(20) NOT NULL,Semester tinyint NOT NULL CHECK(Semester BETWEEN 1 AND 10),EffectiveFrom date NOT NULL,EffectiveTo date NOT NULL,IsActive bit NOT NULL DEFAULT 1,CONSTRAINT CK_Timetable_Time CHECK(EndTime>StartTime),CONSTRAINT CK_Timetable_Dates CHECK(EffectiveTo>=EffectiveFrom),CONSTRAINT UQ_Timetable UNIQUE(RoomId,SubjectId,TeacherId,DayOfWeek,StartTime,AcademicYear,Semester));
CREATE TABLE ClassSessions(SessionId int IDENTITY PRIMARY KEY,TimetableId int NOT NULL REFERENCES Timetables(TimetableId),RoomId int NOT NULL REFERENCES Rooms(RoomId),SubjectId int NOT NULL REFERENCES Subjects(SubjectId),TeacherId int NOT NULL REFERENCES Teachers(TeacherId),SessionDate date NOT NULL,SessionStatus varchar(12) NOT NULL DEFAULT 'Scheduled' CHECK(SessionStatus IN('Scheduled','Held','Cancelled','Rescheduled')),CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),UpdatedAt datetime2 NULL,CONSTRAINT UQ_ClassSession UNIQUE(TimetableId,SessionDate));
CREATE TABLE Attendance(AttendanceId int IDENTITY PRIMARY KEY,SessionId int NULL REFERENCES ClassSessions(SessionId),StudentId int NOT NULL REFERENCES Students(StudentId),RoomId int NOT NULL REFERENCES Rooms(RoomId),SubjectId int NOT NULL REFERENCES Subjects(SubjectId),TeacherId int NOT NULL REFERENCES Teachers(TeacherId),AttendanceDate date NOT NULL,Status varchar(10) NOT NULL CHECK(Status IN('Present','Absent','Late','Excused')),Remark nvarchar(250) NULL,CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),UpdatedAt datetime2 NULL,UpdatedBy int NULL REFERENCES Teachers(TeacherId));
CREATE TABLE MonthlyAttendanceSummary(SummaryId int IDENTITY PRIMARY KEY,StudentId int NOT NULL REFERENCES Students(StudentId),RoomId int NOT NULL REFERENCES Rooms(RoomId),[Month] tinyint NOT NULL CHECK([Month] BETWEEN 1 AND 12),[Year] smallint NOT NULL,TotalSessions int NOT NULL,PresentCount int NOT NULL,AbsentCount int NOT NULL,LateCount int NOT NULL,ExcusedCount int NOT NULL,AttendancePercentage decimal(5,2) NOT NULL,Remark nvarchar(100) NOT NULL,CalculatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),CONSTRAINT UQ_Monthly UNIQUE(StudentId,RoomId,[Month],[Year]));
CREATE INDEX IX_Assignment_TeacherRoom ON TeacherAssignments(TeacherId,RoomId) INCLUDE(SubjectId,IsActive);
CREATE INDEX IX_Students_Room ON Students(RoomId,IsActive);
CREATE INDEX IX_Attendance_RoomDate ON Attendance(RoomId,AttendanceDate) INCLUDE(SubjectId,Status,TeacherId);
CREATE UNIQUE INDEX UX_Attendance_SessionStudent ON Attendance(SessionId,StudentId) WHERE SessionId IS NOT NULL;
CREATE UNIQUE INDEX UX_Attendance_LegacyStudentDate ON Attendance(StudentId,SubjectId,AttendanceDate) WHERE SessionId IS NULL;
CREATE INDEX IX_Timetable_TeacherDay ON Timetables(TeacherId,DayOfWeek,IsActive);
GO
CREATE OR ALTER PROCEDURE RecalculateMonthlyAttendance @RoomId int,@Month tinyint,@Year smallint AS
BEGIN
 SET NOCOUNT ON;
 ;WITH Weighted AS
 (
  SELECT st.StudentId,st.RoomId,a.AttendanceId,a.Status,
   CASE WHEN a.AttendanceId IS NULL THEN 0 WHEN a.SessionId IS NULL THEN 1
        ELSE CAST(CEILING(DATEDIFF(minute,tt.StartTime,tt.EndTime)/60.0) AS int) END SessionHours
  FROM Students st
  LEFT JOIN Attendance a ON a.StudentId=st.StudentId AND MONTH(a.AttendanceDate)=@Month AND YEAR(a.AttendanceDate)=@Year
  LEFT JOIN ClassSessions cs ON cs.SessionId=a.SessionId AND cs.SessionStatus='Held'
  LEFT JOIN Timetables tt ON tt.TimetableId=cs.TimetableId
  WHERE st.RoomId=@RoomId AND st.IsActive=1
 ), C AS
 (
  SELECT StudentId,RoomId,SUM(SessionHours) TotalSessions,
   SUM(CASE WHEN Status='Present' THEN SessionHours ELSE 0 END) PresentCount,
   SUM(CASE WHEN Status='Absent' THEN SessionHours ELSE 0 END) AbsentCount,
   SUM(CASE WHEN Status='Late' THEN SessionHours ELSE 0 END) LateCount,
   SUM(CASE WHEN Status='Excused' THEN SessionHours ELSE 0 END) ExcusedCount,
   CAST(CASE WHEN SUM(SessionHours)-SUM(CASE WHEN Status='Excused' THEN SessionHours ELSE 0 END)=0 THEN 0
        ELSE 100.0*SUM(CASE WHEN Status IN('Present','Late') THEN SessionHours ELSE 0 END)/
        (SUM(SessionHours)-SUM(CASE WHEN Status='Excused' THEN SessionHours ELSE 0 END)) END AS decimal(5,2)) Pct
  FROM Weighted GROUP BY StudentId,RoomId
 )
 MERGE MonthlyAttendanceSummary t USING C s ON t.StudentId=s.StudentId AND t.RoomId=s.RoomId AND t.[Month]=@Month AND t.[Year]=@Year
 WHEN MATCHED THEN UPDATE SET TotalSessions=s.TotalSessions,PresentCount=s.PresentCount,AbsentCount=s.AbsentCount,LateCount=s.LateCount,ExcusedCount=s.ExcusedCount,AttendancePercentage=s.Pct,Remark=CASE WHEN s.Pct<75 THEN 'Attendance below 75%' ELSE 'Satisfactory' END,CalculatedAt=SYSUTCDATETIME()
 WHEN NOT MATCHED THEN INSERT(StudentId,RoomId,[Month],[Year],TotalSessions,PresentCount,AbsentCount,LateCount,ExcusedCount,AttendancePercentage,Remark) VALUES(s.StudentId,s.RoomId,@Month,@Year,s.TotalSessions,s.PresentCount,s.AbsentCount,s.LateCount,s.ExcusedCount,s.Pct,CASE WHEN s.Pct<75 THEN 'Attendance below 75%' ELSE 'Satisfactory' END);
END
GO
-- Create the first administrator securely by running the application once with BootstrapAdmin.ps1.
