SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @RoomId int,@AcademicYear nvarchar(20);
SELECT @RoomId=RoomId,@AcademicYear=AcademicYear FROM Rooms WHERE RoomName=N'CS-3B' AND IsActive=1;
IF @RoomId IS NULL THROW 51000,'Active class CS-3B was not found.',1;

-- Correct the two swapped course codes from the official timetable.
DECLARE @HciSubjectId int,@AdoSubjectId int,@AdoTeacherId int;
SELECT @HciSubjectId=SubjectId FROM Subjects WHERE SubjectName=N'Human Computer Interaction';
SELECT @AdoSubjectId=SubjectId FROM Subjects WHERE SubjectName LIKE N'ADO%';
IF @AdoSubjectId IS NULL
BEGIN
    INSERT Subjects(SubjectName,SubjectCode,CourseYear,IsActive) VALUES(N'ADO.NET, C#',N'TEMP-ADO-3257',3,1);
    SET @AdoSubjectId=SCOPE_IDENTITY();
END;
IF @HciSubjectId IS NULL THROW 51003,'Human Computer Interaction course was not found.',1;
UPDATE Subjects SET SubjectCode=N'TEMP-ADO-3257' WHERE SubjectId=@AdoSubjectId AND SubjectCode<>N'TEMP-ADO-3257';
UPDATE Subjects SET SubjectCode=N'CST-3256',CourseYear=3 WHERE SubjectId=@HciSubjectId;
UPDATE Subjects SET SubjectCode=N'CST-3257',SubjectName=N'ADO.NET, C#',CourseYear=3,IsActive=1 WHERE SubjectId=@AdoSubjectId;
SELECT TOP(1) @AdoTeacherId=t.TeacherId FROM Teachers t JOIN Users u ON u.UserId=t.UserId WHERE t.FullName=N'Daw Hla Hla Htwe' AND u.IsActive=1;
IF @AdoTeacherId IS NULL THROW 51001,'The ADO.NET lecturer Daw Hla Hla Htwe was not found.',1;
IF NOT EXISTS(SELECT 1 FROM TeacherAssignments WHERE TeacherId=@AdoTeacherId AND RoomId=@RoomId AND SubjectId=@AdoSubjectId AND AcademicYear=@AcademicYear)
    INSERT TeacherAssignments(TeacherId,RoomId,SubjectId,AcademicYear,IsActive) VALUES(@AdoTeacherId,@RoomId,@AdoSubjectId,@AcademicYear,1);
ELSE UPDATE TeacherAssignments SET IsActive=1 WHERE TeacherId=@AdoTeacherId AND RoomId=@RoomId AND SubjectId=@AdoSubjectId AND AcademicYear=@AcademicYear;

-- Remove only sample records created by this project. Real attendance is preserved.
DELETE Attendance WHERE RoomId=@RoomId AND
 (Remark=N'July 2026 sample data' OR Remark IN(N'Timetable sample - July 2026',N'Timetable sample - August 2026'));

DECLARE @Schedule TABLE(SubjectCode nvarchar(20),WeekdayNo int);
INSERT @Schedule VALUES
(N'CST-3256',0),(N'CST-3256',1),(N'CST-3256',3),       -- HCI: Mon, Tue, Thu
(N'CST-3257',0),(N'CST-3257',3),                       -- ADO.NET: Mon, Thu
(N'CST-3235',0),(N'CST-3235',2),                       -- Network I: Mon, Wed
(N'CST-3242',1),(N'CST-3242',3),(N'CST-3242',4),       -- Probability: Tue, Thu, Fri
(N'CST-3211',1),(N'CST-3211',2),(N'CST-3211',4),       -- OS: Tue, Wed, Fri
(N'CST-3213',1),(N'CST-3213',2),(N'CST-3213',4),       -- Ethics: Tue, Wed, Fri
(N'CS-3224',1),(N'CS-3224',2),(N'CS-3224',3);          -- SQA: Tue, Wed, Thu

DECLARE @Course TABLE(SubjectCode nvarchar(20) PRIMARY KEY,SubjectId int,TeacherId int);
INSERT @Course
SELECT s.SubjectCode,s.SubjectId,chosen.TeacherId
FROM Subjects s
CROSS APPLY(SELECT TOP(1) ta.TeacherId FROM TeacherAssignments ta JOIN Users u ON u.UserId=(SELECT UserId FROM Teachers WHERE TeacherId=ta.TeacherId) WHERE ta.RoomId=@RoomId AND ta.SubjectId=s.SubjectId AND ta.IsActive=1 AND u.IsActive=1 ORDER BY ta.AssignmentId) chosen
WHERE s.SubjectCode IN(SELECT DISTINCT SubjectCode FROM @Schedule) AND s.IsActive=1;
IF (SELECT COUNT(*) FROM @Course)<>7 THROW 51002,'All seven timetable courses must have an active CS-3B lecturer assignment.',1;

DECLARE @Dates TABLE(AttendanceDate date,MonthNo int,WeekdayNo int);
;WITH Calendar AS
(
 SELECT CAST('2026-07-01' AS date) d
 UNION ALL SELECT DATEADD(day,1,d) FROM Calendar WHERE d<'2026-08-31'
)
INSERT @Dates SELECT d,MONTH(d),DATEDIFF(day,'19000101',d)%7 FROM Calendar WHERE DATEDIFF(day,'19000101',d)%7 BETWEEN 0 AND 4 OPTION(MAXRECURSION 70);

DECLARE @SessionPlan TABLE
(
 AttendanceDate date,
 MonthNo int,
 SubjectId int,
 TeacherId int,
 TimetableId int,
 SessionNo int,
 PRIMARY KEY(AttendanceDate,SubjectId)
);

INSERT @SessionPlan(AttendanceDate,MonthNo,SubjectId,TeacherId,TimetableId,SessionNo)
SELECT d.AttendanceDate,d.MonthNo,c.SubjectId,c.TeacherId,matched.TimetableId,
       ROW_NUMBER() OVER(PARTITION BY d.MonthNo ORDER BY d.AttendanceDate,c.SubjectId)
FROM @Dates d
JOIN @Schedule sc ON sc.WeekdayNo=d.WeekdayNo
JOIN @Course c ON c.SubjectCode=sc.SubjectCode
CROSS APPLY
(
 SELECT TOP(1) tt.TimetableId
 FROM Timetables tt
 WHERE tt.RoomId=@RoomId AND tt.SubjectId=c.SubjectId AND tt.TeacherId=c.TeacherId
   AND tt.DayOfWeek=d.WeekdayNo+1
   AND d.AttendanceDate BETWEEN tt.EffectiveFrom AND tt.EffectiveTo
 ORDER BY tt.IsActive DESC,tt.StartTime,tt.TimetableId
) matched;

DECLARE @ExpectedSessions int=(SELECT COUNT(*) FROM @Dates d JOIN @Schedule sc ON sc.WeekdayNo=d.WeekdayNo);
IF (SELECT COUNT(*) FROM @SessionPlan)<>@ExpectedSessions
    THROW 51004,'Some sample dates do not have a matching CS-3B timetable session. Run the timetable migration first.',1;

INSERT ClassSessions(TimetableId,RoomId,SubjectId,TeacherId,SessionDate,SessionStatus)
SELECT p.TimetableId,@RoomId,p.SubjectId,p.TeacherId,p.AttendanceDate,'Held'
FROM @SessionPlan p
WHERE NOT EXISTS
(
 SELECT 1 FROM ClassSessions cs
 WHERE cs.TimetableId=p.TimetableId AND cs.SessionDate=p.AttendanceDate
);

UPDATE cs SET SessionStatus='Held',UpdatedAt=SYSUTCDATETIME()
FROM ClassSessions cs
JOIN @SessionPlan p ON p.TimetableId=cs.TimetableId AND p.AttendanceDate=cs.SessionDate
WHERE cs.SessionStatus<>'Held';

;WITH ScheduledSessions AS
(
 SELECT p.AttendanceDate,p.MonthNo,p.SubjectId,p.TeacherId,p.SessionNo,cs.SessionId
 FROM @SessionPlan p
 JOIN ClassSessions cs ON cs.TimetableId=p.TimetableId AND cs.SessionDate=p.AttendanceDate
), RankedStudents AS
(
 SELECT StudentId,ROW_NUMBER() OVER(ORDER BY StudentCode) StudentNo FROM Students WHERE RoomId=@RoomId AND IsActive=1
), Sample AS
(
 SELECT st.StudentId,ss.SessionId,ss.SubjectId,ss.TeacherId,ss.AttendanceDate,ss.MonthNo,
 CASE
  WHEN st.StudentNo<=10 AND ss.SessionNo%10 BETWEEN 1 AND 6 THEN 'Present'
  WHEN st.StudentNo<=10 AND ss.SessionNo%10=7 THEN 'Present'
  WHEN st.StudentNo<=10 THEN 'Absent'
  WHEN st.StudentNo<=20 AND ss.SessionNo%10 BETWEEN 1 AND 8 THEN 'Present'
  WHEN st.StudentNo<=20 AND ss.SessionNo%10=9 THEN 'Present'
  WHEN st.StudentNo<=20 THEN 'Absent'
  WHEN ss.SessionNo%10 BETWEEN 1 AND 7 THEN 'Present'
  WHEN ss.SessionNo%10=8 THEN 'Present'
  ELSE 'Absent' END [Status]
 FROM RankedStudents st CROSS JOIN ScheduledSessions ss
)
INSERT Attendance(SessionId,StudentId,RoomId,SubjectId,TeacherId,AttendanceDate,[Status],Remark,UpdatedBy)
SELECT x.SessionId,x.StudentId,@RoomId,x.SubjectId,x.TeacherId,x.AttendanceDate,x.[Status],
       CASE x.MonthNo WHEN 7 THEN N'Timetable sample - July 2026' ELSE N'Timetable sample - August 2026' END,x.TeacherId
FROM Sample x
WHERE NOT EXISTS(SELECT 1 FROM Attendance a WHERE a.StudentId=x.StudentId AND a.SubjectId=x.SubjectId AND a.AttendanceDate=x.AttendanceDate);

DECLARE @Inserted int=@@ROWCOUNT;
COMMIT TRANSACTION;

EXEC RecalculateMonthlyAttendance @RoomId=@RoomId,@Month=7,@Year=2026;
EXEC RecalculateMonthlyAttendance @RoomId=@RoomId,@Month=8,@Year=2026;
SELECT @Inserted InsertedAttendanceRows;
