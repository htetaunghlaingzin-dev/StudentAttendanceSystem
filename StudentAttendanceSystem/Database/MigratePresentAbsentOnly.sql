USE StudentAttendanceSystem;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE dbo.Attendance SET Status='Present' WHERE Status='Late';
UPDATE dbo.Attendance SET Status='Absent' WHERE Status='Excused';

UPDATE dbo.MonthlyAttendanceSummary
SET PresentCount=PresentCount+LateCount,
    AbsentCount=AbsentCount+ExcusedCount,
    LateCount=0,
    ExcusedCount=0;

DECLARE @drop nvarchar(max)=N'';
SELECT @drop+=N'ALTER TABLE dbo.Attendance DROP CONSTRAINT '+QUOTENAME(cc.name)+N';'
FROM sys.check_constraints cc
WHERE cc.parent_object_id=OBJECT_ID(N'dbo.Attendance') AND cc.definition LIKE N'%Status%';
EXEC sys.sp_executesql @drop;

ALTER TABLE dbo.Attendance ADD CONSTRAINT CK_Attendance_Status CHECK(Status IN('Present','Absent'));
COMMIT TRANSACTION;
GO

CREATE OR ALTER PROCEDURE dbo.RecalculateMonthlyAttendance @RoomId int,@Month tinyint,@Year smallint AS
BEGIN
 SET NOCOUNT ON;
 ;WITH Weighted AS
 (
  SELECT st.StudentId,st.RoomId,a.AttendanceId,a.Status,
   CASE WHEN a.AttendanceId IS NULL THEN 0 WHEN a.SessionId IS NULL THEN 1
        ELSE CAST(CEILING(DATEDIFF(minute,tt.StartTime,tt.EndTime)/60.0) AS int) END SessionHours
  FROM dbo.Students st
  LEFT JOIN dbo.Attendance a ON a.StudentId=st.StudentId AND MONTH(a.AttendanceDate)=@Month AND YEAR(a.AttendanceDate)=@Year
  LEFT JOIN dbo.ClassSessions cs ON cs.SessionId=a.SessionId AND cs.SessionStatus='Held'
  LEFT JOIN dbo.Timetables tt ON tt.TimetableId=cs.TimetableId
  WHERE st.RoomId=@RoomId AND st.IsActive=1
 ), X AS
 (
  SELECT StudentId,RoomId,
   CAST(SUM(SessionHours) AS int) TotalSessions,
   CAST(SUM(CASE WHEN Status='Present' THEN SessionHours ELSE 0 END) AS int) PresentCount,
   CAST(SUM(CASE WHEN Status='Absent' THEN SessionHours ELSE 0 END) AS int) AbsentCount,
   CAST(CASE WHEN SUM(SessionHours)=0 THEN 0 ELSE 100.0*SUM(CASE WHEN Status='Present' THEN SessionHours ELSE 0 END)/SUM(SessionHours) END AS decimal(5,2)) Pct
  FROM Weighted
  GROUP BY StudentId,RoomId
 )
 MERGE dbo.MonthlyAttendanceSummary t USING X s ON t.StudentId=s.StudentId AND t.RoomId=s.RoomId AND t.[Month]=@Month AND t.[Year]=@Year
 WHEN MATCHED THEN UPDATE SET TotalSessions=s.TotalSessions,PresentCount=s.PresentCount,AbsentCount=s.AbsentCount,LateCount=0,ExcusedCount=0,AttendancePercentage=s.Pct,Remark=CASE WHEN s.Pct<75 THEN 'Attendance below 75%' ELSE 'Satisfactory' END,CalculatedAt=SYSUTCDATETIME()
 WHEN NOT MATCHED THEN INSERT(StudentId,RoomId,[Month],[Year],TotalSessions,PresentCount,AbsentCount,LateCount,ExcusedCount,AttendancePercentage,Remark) VALUES(s.StudentId,s.RoomId,@Month,@Year,s.TotalSessions,s.PresentCount,s.AbsentCount,0,0,s.Pct,CASE WHEN s.Pct<75 THEN 'Attendance below 75%' ELSE 'Satisfactory' END);
END;
GO
