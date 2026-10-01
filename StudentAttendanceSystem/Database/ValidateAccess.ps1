Add-Type -AssemblyName System.Data
$p='C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem\Database\StudentAttendanceSystem.mdb'
$c=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$p;";$c.Open()
foreach($t in 'Users','Teachers','Rooms','Subjects','TeacherAssignments','Students','Timetables','ClassSessions','Attendance','MonthlyAttendanceSummary'){$cmd=$c.CreateCommand();$cmd.CommandText="SELECT COUNT(*) FROM [$t]";"$t=$($cmd.ExecuteScalar())"};$c.Close()
