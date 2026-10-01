Add-Type -AssemblyName System.Data
$p='C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem\StudentAttendanceSystem-VS2012-Access\StudentAttendanceSystem.mdb';$c=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$p;";$c.Open()
$q=@(
'SELECT COUNT(*) AS [Value] FROM Students WHERE IsActive=True',
'SELECT COUNT(*) AS [Value] FROM Subjects WHERE IsActive=True',
'SELECT COUNT(*) AS [Value] FROM Attendance WHERE AttendanceDate>=DateSerial(Year(Date()),Month(Date()),1)',
'SELECT u.UserId,t.TeacherCode AS [Code],t.FullName AS [Name],t.Phone,u.Username,u.IsActive AS [Active] FROM Teachers t INNER JOIN Users u ON u.UserId=t.UserId ORDER BY u.UserId',
'SELECT r.RoomId,r.RoomName AS [Class],r.Department,r.ProgramName AS [Program],r.StudyLevel AS [Year],r.Semester,r.AcademicYear,t.FullName AS [HomeTeacher],r.IsActive AS [Active] FROM Rooms r LEFT JOIN Teachers t ON t.TeacherId=r.HomeRoomTeacherId ORDER BY r.RoomId',
'SELECT ta.AssignmentId,t.FullName AS [Lecturer],r.RoomName AS [Class],s.SubjectCode+'' - ''+s.SubjectName AS [Course],ta.AcademicYear,ta.IsActive AS [Active] FROM TeacherAssignments ta INNER JOIN Teachers t ON t.TeacherId=ta.TeacherId INNER JOIN Rooms r ON r.RoomId=ta.RoomId INNER JOIN Subjects s ON s.SubjectId=ta.SubjectId ORDER BY ta.AssignmentId',
'SELECT tt.TimetableId,r.RoomName AS [Class],s.SubjectCode+'' - ''+s.SubjectName AS [Course],t.FullName AS [Lecturer],CHOOSE(tt.DayOfWeek,''Monday'',''Tuesday'',''Wednesday'',''Thursday'',''Friday'',''Saturday'',''Sunday'') AS [Day],tt.StartTime,tt.EndTime,tt.EffectiveFrom,tt.EffectiveTo,tt.IsActive AS [Active] FROM Timetables tt INNER JOIN Rooms r ON r.RoomId=tt.RoomId INNER JOIN Subjects s ON s.SubjectId=tt.SubjectId INNER JOIN Teachers t ON t.TeacherId=tt.TeacherId ORDER BY tt.DayOfWeek,tt.StartTime,tt.TimetableId',
'SELECT SubjectId,SubjectCode AS [Code],SubjectName AS [Course],CourseYear AS [Year],IsActive AS [Active] FROM Subjects ORDER BY SubjectId',
'SELECT RoomId AS [Id],RoomName AS [Name] FROM Rooms WHERE IsActive=True ORDER BY RoomName')
foreach($sql in $q){try{$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$r=$cmd.ExecuteReader();$r.Close();"OK"}catch{"ERROR: $($_.Exception.Message)`n$sql"}};$c.Close()
