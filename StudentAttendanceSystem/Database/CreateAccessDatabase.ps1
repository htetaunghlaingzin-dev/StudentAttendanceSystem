Add-Type -AssemblyName System.Data
function JetValue($v){if($null -eq $v -or $v -is [DBNull]){return 'NULL'};if($v -is [string]){return "'"+$v.Replace("'","''")+"'"};if($v -is [bool]){if($v){return 'True'}else{return 'False'}};if($v -is [TimeSpan]){$v=([datetime]'1899-12-30').Add($v)};if($v -is [datetime]){return '#'+$v.ToString('yyyy-MM-dd HH:mm:ss')+'#'};return [Convert]::ToString($v,[Globalization.CultureInfo]::InvariantCulture)}
$root='C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem'
$out=Join-Path $root 'Database\StudentAttendanceSystem.mdb'
if(Test-Path $out){Remove-Item -LiteralPath $out -Force}
$cat=New-Object -ComObject ADOX.Catalog
$cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$out;Jet OLEDB:Engine Type=5")
$cat.ActiveConnection.Close();[Runtime.InteropServices.Marshal]::ReleaseComObject($cat)|Out-Null
[xml]$cfg=Get-Content (Join-Path $root 'StudentAttendanceSystem-VS2012\App.config')
$source=New-Object Data.SqlClient.SqlConnection ([string]$cfg.configuration.connectionStrings.add.connectionString);$source.Open()
$dest=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$out;";$dest.Open()
$ddl=@(
'CREATE TABLE Users (UserId LONG CONSTRAINT PK_Users PRIMARY KEY,Username TEXT(50),PasswordHash MEMO,FullName TEXT(120),[Role] TEXT(10),IsActive YESNO,CreatedAt DATETIME)',
'CREATE TABLE Teachers (TeacherId LONG CONSTRAINT PK_Teachers PRIMARY KEY,UserId LONG,TeacherCode TEXT(30),FullName TEXT(120),Phone TEXT(30))',
'CREATE TABLE Rooms (RoomId LONG CONSTRAINT PK_Rooms PRIMARY KEY,RoomName TEXT(80),Department TEXT(100),ProgramName TEXT(120),StudyLevel TEXT(30),Semester BYTE,AcademicYear TEXT(20),HomeRoomTeacherId LONG,IsActive YESNO)',
'CREATE TABLE Subjects (SubjectId LONG CONSTRAINT PK_Subjects PRIMARY KEY,SubjectName TEXT(80),SubjectCode TEXT(20),CourseYear BYTE,IsActive YESNO)',
'CREATE TABLE TeacherAssignments (AssignmentId LONG CONSTRAINT PK_Assignments PRIMARY KEY,TeacherId LONG,RoomId LONG,SubjectId LONG,AcademicYear TEXT(20),IsActive YESNO)',
'CREATE TABLE Students (StudentId LONG CONSTRAINT PK_Students PRIMARY KEY,StudentCode TEXT(30),StudentName TEXT(120),Gender TEXT(15),RoomId LONG,AcademicYear TEXT(20),IsActive YESNO)',
'CREATE TABLE Timetables (TimetableId LONG CONSTRAINT PK_Timetables PRIMARY KEY,RoomId LONG,SubjectId LONG,TeacherId LONG,DayOfWeek BYTE,StartTime DATETIME,EndTime DATETIME,AcademicYear TEXT(20),Semester BYTE,EffectiveFrom DATETIME,EffectiveTo DATETIME,IsActive YESNO)',
'CREATE TABLE ClassSessions (SessionId LONG CONSTRAINT PK_Sessions PRIMARY KEY,TimetableId LONG,RoomId LONG,SubjectId LONG,TeacherId LONG,SessionDate DATETIME,SessionStatus TEXT(12),CreatedAt DATETIME,UpdatedAt DATETIME)',
'CREATE TABLE Attendance (AttendanceId LONG CONSTRAINT PK_Attendance PRIMARY KEY,SessionId LONG,StudentId LONG,RoomId LONG,SubjectId LONG,TeacherId LONG,AttendanceDate DATETIME,[Status] TEXT(10),Remark TEXT(250),CreatedAt DATETIME,UpdatedAt DATETIME,UpdatedBy LONG)',
'CREATE TABLE MonthlyAttendanceSummary (SummaryId LONG CONSTRAINT PK_Summary PRIMARY KEY,StudentId LONG,RoomId LONG,[Month] BYTE,[Year] INTEGER,TotalSessions LONG,PresentCount LONG,AbsentCount LONG,LateCount LONG,ExcusedCount LONG,AttendancePercentage DOUBLE,Remark TEXT(100),CalculatedAt DATETIME)'
)
foreach($sql in $ddl){$cmd=$dest.CreateCommand();$cmd.CommandText=$sql;$cmd.ExecuteNonQuery()|Out-Null}
$tables='Users','Teachers','Rooms','Subjects','TeacherAssignments','Students','Timetables','ClassSessions','Attendance','MonthlyAttendanceSummary'
$tx=$dest.BeginTransaction()
try{
 foreach($table in $tables){
  $read=$source.CreateCommand();$read.CommandText="SELECT * FROM dbo.[$table]";$reader=$read.ExecuteReader()
  $names=@();for($i=0;$i -lt $reader.FieldCount;$i++){$names+='['+$reader.GetName($i)+']'}
  $prefix="INSERT INTO [$table] ("+($names -join ',')+") VALUES ("
  $rowNumber=0
  while($reader.Read()){$rowNumber++;$values=@();for($i=0;$i -lt $reader.FieldCount;$i++){$values+=JetValue $reader.GetValue($i)};$insert=$dest.CreateCommand();$insert.Transaction=$tx;$insert.CommandText=$prefix+($values -join ',')+')';try{$insert.ExecuteNonQuery()|Out-Null}catch{throw "Import failed for $table row ${rowNumber}: $($_.Exception.Message)"}}
  $reader.Close()
 }
 $tx.Commit()
}catch{$tx.Rollback();throw}
foreach($sql in @('CREATE UNIQUE INDEX UX_Users_Username ON Users(Username)','CREATE UNIQUE INDEX UX_Teachers_User ON Teachers(UserId)','CREATE UNIQUE INDEX UX_Teachers_Code ON Teachers(TeacherCode)','CREATE UNIQUE INDEX UX_Subjects_Code ON Subjects(SubjectCode)','CREATE INDEX IX_Students_Room ON Students(RoomId)','CREATE INDEX IX_Attendance_RoomDate ON Attendance(RoomId,AttendanceDate)','CREATE INDEX IX_Timetable_TeacherDay ON Timetables(TeacherId,DayOfWeek)')){$cmd=$dest.CreateCommand();$cmd.CommandText=$sql;$cmd.ExecuteNonQuery()|Out-Null}
$dest.Close();$source.Close()
Get-Item $out|Select-Object FullName,Length

