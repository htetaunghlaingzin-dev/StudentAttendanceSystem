Add-Type -AssemblyName System.Data
$source=Get-Content 'C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem\StudentAttendanceSystem-VS2012-Access\PasswordHasher.cs' -Raw
$source=$source.Replace('static class PasswordHasher','public static class PasswordHasher')
Add-Type -TypeDefinition $source
$p='C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem\StudentAttendanceSystem-VS2012-Access\bin\Debug\StudentAttendanceSystem.mdb'
$c=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$p;";$c.Open();$cmd=$c.CreateCommand();$cmd.CommandText='SELECT PasswordHash FROM Users WHERE Username=? AND IsActive=True';$parameter=$cmd.Parameters.Add('?', [Data.OleDb.OleDbType]::VarChar,50);$parameter.Value='admin';$hash=[string]$cmd.ExecuteScalar();$c.Close();"Login lookup found=$(-not [string]::IsNullOrEmpty($hash)); Password verified=$([StudentAttendanceSystemVS2012.PasswordHasher]::Verify('Admin@123',$hash))"
