USE StudentAttendanceSystem;
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID('dbo.Timetables','U') IS NULL
CREATE TABLE dbo.Timetables(TimetableId int IDENTITY PRIMARY KEY,RoomId int NOT NULL REFERENCES dbo.Rooms(RoomId),SubjectId int NOT NULL REFERENCES dbo.Subjects(SubjectId),TeacherId int NOT NULL REFERENCES dbo.Teachers(TeacherId),DayOfWeek tinyint NOT NULL CHECK(DayOfWeek BETWEEN 1 AND 7),StartTime time(0) NOT NULL,EndTime time(0) NOT NULL,AcademicYear nvarchar(20) NOT NULL,Semester tinyint NOT NULL CHECK(Semester BETWEEN 1 AND 10),EffectiveFrom date NOT NULL,EffectiveTo date NOT NULL,IsActive bit NOT NULL DEFAULT 1,CONSTRAINT CK_Timetable_Time CHECK(EndTime>StartTime),CONSTRAINT CK_Timetable_Dates CHECK(EffectiveTo>=EffectiveFrom),CONSTRAINT UQ_Timetable UNIQUE(RoomId,SubjectId,TeacherId,DayOfWeek,StartTime,AcademicYear,Semester));
GO
IF OBJECT_ID('dbo.ClassSessions','U') IS NULL
CREATE TABLE dbo.ClassSessions(SessionId int IDENTITY PRIMARY KEY,TimetableId int NOT NULL REFERENCES dbo.Timetables(TimetableId),RoomId int NOT NULL REFERENCES dbo.Rooms(RoomId),SubjectId int NOT NULL REFERENCES dbo.Subjects(SubjectId),TeacherId int NOT NULL REFERENCES dbo.Teachers(TeacherId),SessionDate date NOT NULL,SessionStatus varchar(12) NOT NULL DEFAULT 'Scheduled' CHECK(SessionStatus IN('Scheduled','Held','Cancelled','Rescheduled')),CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),UpdatedAt datetime2 NULL,CONSTRAINT UQ_ClassSession UNIQUE(TimetableId,SessionDate));
GO
IF COL_LENGTH('dbo.Attendance','SessionId') IS NULL ALTER TABLE dbo.Attendance ADD SessionId int NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Attendance_ClassSessions') ALTER TABLE dbo.Attendance ADD CONSTRAINT FK_Attendance_ClassSessions FOREIGN KEY(SessionId) REFERENCES dbo.ClassSessions(SessionId);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Attendance_SessionStudent' AND object_id=OBJECT_ID('dbo.Attendance')) CREATE UNIQUE INDEX UX_Attendance_SessionStudent ON dbo.Attendance(SessionId,StudentId) WHERE SessionId IS NOT NULL;
IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name='UQ_Attendance' AND parent_object_id=OBJECT_ID('dbo.Attendance')) ALTER TABLE dbo.Attendance DROP CONSTRAINT UQ_Attendance;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Attendance_LegacyStudentDate' AND object_id=OBJECT_ID('dbo.Attendance')) CREATE UNIQUE INDEX UX_Attendance_LegacyStudentDate ON dbo.Attendance(StudentId,SubjectId,AttendanceDate) WHERE SessionId IS NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Timetable_TeacherDay' AND object_id=OBJECT_ID('dbo.Timetables')) CREATE INDEX IX_Timetable_TeacherDay ON dbo.Timetables(TeacherId,DayOfWeek,IsActive);
GO
PRINT 'Timetable and class-session schema created successfully.';
