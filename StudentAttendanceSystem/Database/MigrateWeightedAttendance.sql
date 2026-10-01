USE StudentAttendanceSystem;
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
   0 LateCount,
   0 ExcusedCount,
   CAST(CASE WHEN SUM(SessionHours)=0 THEN 0 ELSE 100.0*SUM(CASE WHEN Status='Present' THEN SessionHours ELSE 0 END)/SUM(SessionHours) END AS decimal(5,2)) Pct
  FROM Weighted GROUP BY StudentId,RoomId
 )
 MERGE MonthlyAttendanceSummary t USING C s ON t.StudentId=s.StudentId AND t.RoomId=s.RoomId AND t.[Month]=@Month AND t.[Year]=@Year
 WHEN MATCHED THEN UPDATE SET TotalSessions=s.TotalSessions,PresentCount=s.PresentCount,AbsentCount=s.AbsentCount,LateCount=s.LateCount,ExcusedCount=s.ExcusedCount,AttendancePercentage=s.Pct,Remark=CASE WHEN s.Pct<75 THEN 'Attendance below 75%' ELSE 'Satisfactory' END,CalculatedAt=SYSUTCDATETIME()
 WHEN NOT MATCHED THEN INSERT(StudentId,RoomId,[Month],[Year],TotalSessions,PresentCount,AbsentCount,LateCount,ExcusedCount,AttendancePercentage,Remark) VALUES(s.StudentId,s.RoomId,@Month,@Year,s.TotalSessions,s.PresentCount,s.AbsentCount,s.LateCount,s.ExcusedCount,s.Pct,CASE WHEN s.Pct<75 THEN 'Attendance below 75%' ELSE 'Satisfactory' END);
END
GO
PRINT 'Attendance calculation now uses timetable hours.';
