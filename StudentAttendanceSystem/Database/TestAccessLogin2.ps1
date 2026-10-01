Add-Type -AssemblyName System.Data
$p='C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem\StudentAttendanceSystem-VS2012-Access\bin\Debug\StudentAttendanceSystem.mdb'
$c=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$p;";$c.Open();$cmd=$c.CreateCommand();$cmd.CommandText='SELECT u.UserId,u.Username,u.PasswordHash,u.FullName,u.Role,t.TeacherId FROM Users u LEFT JOIN Teachers t ON t.UserId=u.UserId WHERE u.Username=''admin'' AND u.IsActive=True';$a=New-Object Data.OleDb.OleDbDataAdapter $cmd;$dt=New-Object Data.DataTable;[void]$a.Fill($dt);"Login rows=$($dt.Rows.Count) role=$($dt.Rows[0]['Role'])";$c.Close()

