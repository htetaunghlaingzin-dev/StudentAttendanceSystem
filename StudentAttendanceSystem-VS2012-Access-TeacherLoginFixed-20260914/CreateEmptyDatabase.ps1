param([string]$OutputPath=(Join-Path $PSScriptRoot 'StudentAttendanceSystem-Empty.mdb'))
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Data
if(Test-Path -LiteralPath $OutputPath){Remove-Item -LiteralPath $OutputPath -Force}
$cat=New-Object -ComObject ADOX.Catalog;$cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$OutputPath;Jet OLEDB:Engine Type=5");$cat.ActiveConnection.Close();[Runtime.InteropServices.Marshal]::ReleaseComObject($cat)|Out-Null
$c=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$OutputPath;";$c.Open()
$ddl=@(
'CREATE TABLE Users (UserId COUNTER CONSTRAINT PK_Users PRIMARY KEY,Username TEXT(50),PasswordHash MEMO,FullName TEXT(120),[Role] TEXT(10),IsActive YESNO,CreatedAt DATETIME)',
'CREATE TABLE Teachers (TeacherId COUNTER CONSTRAINT PK_Teachers PRIMARY KEY,UserId LONG,TeacherCode TEXT(30),FullName TEXT(120),Phone TEXT(30))',
'CREATE TABLE Rooms (RoomId COUNTER CONSTRAINT PK_Rooms PRIMARY KEY,RoomName TEXT(80),Department TEXT(100),ProgramName TEXT(120),StudyLevel TEXT(30),Semester BYTE,AcademicYear TEXT(20),HomeRoomTeacherId LONG,IsActive YESNO)',
'CREATE TABLE Subjects (SubjectId COUNTER CONSTRAINT PK_Subjects PRIMARY KEY,SubjectName TEXT(80),SubjectCode TEXT(20),CourseYear BYTE,IsActive YESNO)',
'CREATE TABLE TeacherAssignments (AssignmentId COUNTER CONSTRAINT PK_Assignments PRIMARY KEY,TeacherId LONG,RoomId LONG,SubjectId LONG,AcademicYear TEXT(20),IsActive YESNO)',
'CREATE TABLE Students (StudentId COUNTER CONSTRAINT PK_Students PRIMARY KEY,StudentCode TEXT(30),StudentName TEXT(120),Gender TEXT(15),RoomId LONG,AcademicYear TEXT(20),IsActive YESNO)',
'CREATE TABLE Timetables (TimetableId COUNTER CONSTRAINT PK_Timetables PRIMARY KEY,RoomId LONG,SubjectId LONG,TeacherId LONG,DayOfWeek BYTE,StartTime DATETIME,EndTime DATETIME,AcademicYear TEXT(20),Semester BYTE,EffectiveFrom DATETIME,EffectiveTo DATETIME,IsActive YESNO)',
'CREATE TABLE ClassSessions (SessionId COUNTER CONSTRAINT PK_Sessions PRIMARY KEY,TimetableId LONG,RoomId LONG,SubjectId LONG,TeacherId LONG,SessionDate DATETIME,SessionStatus TEXT(12),CreatedAt DATETIME,UpdatedAt DATETIME)',
'CREATE TABLE Attendance (AttendanceId COUNTER CONSTRAINT PK_Attendance PRIMARY KEY,SessionId LONG,StudentId LONG,RoomId LONG,SubjectId LONG,TeacherId LONG,AttendanceDate DATETIME,[Status] TEXT(10),Remark TEXT(250),CreatedAt DATETIME,UpdatedAt DATETIME,UpdatedBy LONG)',
'CREATE TABLE MonthlyAttendanceSummary (SummaryId COUNTER CONSTRAINT PK_Summary PRIMARY KEY,StudentId LONG,RoomId LONG,[Month] BYTE,[Year] INTEGER,TotalSessions LONG,PresentCount LONG,AbsentCount LONG,LateCount LONG,ExcusedCount LONG,AttendancePercentage DOUBLE,Remark TEXT(100),CalculatedAt DATETIME)')
foreach($sql in $ddl){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.ExecuteNonQuery()|Out-Null}
foreach($sql in @('CREATE UNIQUE INDEX UX_Users_Username ON Users(Username)','CREATE UNIQUE INDEX UX_Teachers_User ON Teachers(UserId)','CREATE UNIQUE INDEX UX_Teachers_Code ON Teachers(TeacherCode)','CREATE UNIQUE INDEX UX_Subjects_Code ON Subjects(SubjectCode)','CREATE INDEX IX_Students_Room ON Students(RoomId)','CREATE INDEX IX_Attendance_RoomDate ON Attendance(RoomId,AttendanceDate)','CREATE INDEX IX_Timetable_TeacherDay ON Timetables(TeacherId,DayOfWeek)')){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.ExecuteNonQuery()|Out-Null}
$cmd=$c.CreateCommand();$cmd.CommandText="INSERT INTO Users(Username,PasswordHash,FullName,[Role],IsActive,CreatedAt) VALUES('admin','RESET_REQUIRED','System Administrator','Admin',True,Now())";$cmd.ExecuteNonQuery()|Out-Null;$c.Close();Write-Host $OutputPath
