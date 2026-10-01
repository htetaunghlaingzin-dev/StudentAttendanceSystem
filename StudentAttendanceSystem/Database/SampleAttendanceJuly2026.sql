SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @RoomId int,@SubjectId int,@TeacherId int;
SELECT TOP(1) @RoomId=r.RoomId,@SubjectId=s.SubjectId,@TeacherId=t.TeacherId
FROM TeacherAssignments ta
JOIN Rooms r ON r.RoomId=ta.RoomId
JOIN Subjects s ON s.SubjectId=ta.SubjectId
JOIN Teachers t ON t.TeacherId=ta.TeacherId
JOIN Users u ON u.UserId=t.UserId
WHERE r.RoomName=N'CS-3B' AND s.SubjectName=N'Human Computer Interaction'
  AND ta.IsActive=1 AND r.IsActive=1 AND s.IsActive=1 AND u.IsActive=1
ORDER BY ta.AssignmentId;

IF @RoomId IS NULL THROW 51000,'An active CS-3B Human Computer Interaction assignment was not found.',1;

DECLARE @Dates TABLE(SessionNo int PRIMARY KEY,AttendanceDate date UNIQUE);
;WITH Calendar AS
(
    SELECT CAST('2026-07-01' AS date) AttendanceDate
    UNION ALL SELECT DATEADD(day,1,AttendanceDate) FROM Calendar WHERE AttendanceDate<'2026-07-31'
), Weekdays AS
(
    SELECT AttendanceDate,ROW_NUMBER() OVER(ORDER BY AttendanceDate) SessionNo
    FROM Calendar WHERE DATEDIFF(day,'19000101',AttendanceDate)%7 BETWEEN 0 AND 4
)
INSERT @Dates SELECT SessionNo,AttendanceDate FROM Weekdays WHERE SessionNo<=20 OPTION(MAXRECURSION 40);

IF (SELECT COUNT(*) FROM @Dates)<>20 THROW 51001,'Expected exactly 20 sample sessions.',1;

;WITH RankedStudents AS
(
    SELECT StudentId,ROW_NUMBER() OVER(ORDER BY StudentCode) StudentNo
    FROM Students WHERE RoomId=@RoomId AND IsActive=1
), Sample AS
(
    SELECT st.StudentId,d.AttendanceDate,
      CASE
        WHEN st.StudentNo<=10 AND d.SessionNo<=12 THEN 'Present'
        WHEN st.StudentNo<=10 AND d.SessionNo<=14 THEN 'Present'
        WHEN st.StudentNo<=10 THEN 'Absent'
        WHEN st.StudentNo<=20 AND d.SessionNo<=16 THEN 'Present'
        WHEN st.StudentNo<=20 AND d.SessionNo<=18 THEN 'Present'
        WHEN st.StudentNo<=20 THEN 'Absent'
        WHEN d.SessionNo<=14 THEN 'Present'
        WHEN d.SessionNo<=16 THEN 'Present'
        ELSE 'Absent'
      END [Status]
    FROM RankedStudents st CROSS JOIN @Dates d
)
INSERT Attendance(StudentId,RoomId,SubjectId,TeacherId,AttendanceDate,[Status],Remark,UpdatedBy)
SELECT x.StudentId,@RoomId,@SubjectId,@TeacherId,x.AttendanceDate,x.[Status],N'July 2026 sample data',@TeacherId
FROM Sample x
WHERE NOT EXISTS(SELECT 1 FROM Attendance a WHERE a.StudentId=x.StudentId AND a.SubjectId=@SubjectId AND a.AttendanceDate=x.AttendanceDate);

DECLARE @Inserted int=@@ROWCOUNT;
COMMIT TRANSACTION;

EXEC RecalculateMonthlyAttendance @RoomId=@RoomId,@Month=7,@Year=2026;
SELECT @Inserted InsertedAttendanceRows,@RoomId RoomId,@SubjectId SubjectId,@TeacherId TeacherId;
