Add-Type -AssemblyName System.Data
[xml]$cfg=Get-Content 'StudentAttendanceSystem-VS2012\App.config'
$connectionString=[string]$cfg.configuration.connectionStrings.add.connectionString
$connection=New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
function SqlLiteral($value,[string]$typeName){
 if($null -eq $value -or $value -is [DBNull]){return 'NULL'}
 if($typeName -in @('nvarchar','nchar','ntext')){return "N'"+([string]$value).Replace("'","''")+"'"}
 if($typeName -in @('varchar','char','text')){return "'"+([string]$value).Replace("'","''")+"'"}
 if($typeName -eq 'date'){return "'"+([datetime]$value).ToString('yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture)+"'"}
 if($typeName -in @('datetime','datetime2','smalldatetime')){return "'"+([datetime]$value).ToString('yyyy-MM-ddTHH:mm:ss.fffffff',[Globalization.CultureInfo]::InvariantCulture)+"'"}
 if($typeName -eq 'time'){return "'"+([timespan]$value).ToString('hh\:mm\:ss',[Globalization.CultureInfo]::InvariantCulture)+"'"}
 if($typeName -eq 'bit'){if([bool]$value){return '1'}else{return '0'}}
 if($typeName -in @('binary','varbinary','image')){return '0x'+([BitConverter]::ToString([byte[]]$value).Replace('-',''))}
 if($typeName -eq 'uniqueidentifier'){return "'"+$value.ToString()+"'"}
 return [Convert]::ToString($value,[Globalization.CultureInfo]::InvariantCulture)
}
$setup=[IO.File]::ReadAllText((Resolve-Path 'Database\Setup.sql'))
$schemaStart=$setup.IndexOf('CREATE TABLE Users')
$procStart=$setup.IndexOf('CREATE OR ALTER PROCEDURE')
if($schemaStart -lt 0 -or $procStart -lt 0){throw 'Setup.sql format is unexpected.'}
$schema=$setup.Substring($schemaStart,$procStart-$schemaStart)
$procedure=$setup.Substring($procStart)
$procedure=$procedure.Substring(0,$procedure.IndexOf('-- Create the first administrator'))
$builder=New-Object Text.StringBuilder
[void]$builder.AppendLine('-- Student Attendance System - complete database schema and current data')
[void]$builder.AppendLine('-- Generated 2026-09-12. Run this file on SQL Server using an account that can create databases.')
[void]$builder.AppendLine('SET NOCOUNT ON;')
[void]$builder.AppendLine("IF DB_ID(N'StudentAttendanceSystem') IS NULL CREATE DATABASE StudentAttendanceSystem;")
[void]$builder.AppendLine('GO')
[void]$builder.AppendLine('USE StudentAttendanceSystem;')
[void]$builder.AppendLine('GO')
[void]$builder.AppendLine("IF OBJECT_ID(N'dbo.Users',N'U') IS NOT NULL THROW 51000, 'StudentAttendanceSystem already contains tables. Use a new/empty database before running this complete installation file.', 1;")
[void]$builder.AppendLine('GO')
[void]$builder.AppendLine($schema.Trim())
[void]$builder.AppendLine('GO')
[void]$builder.AppendLine('-- Current project data')
$tables=@('Users','Teachers','Rooms','Subjects','TeacherAssignments','Students','Timetables','ClassSessions','Attendance','MonthlyAttendanceSummary')
foreach($table in $tables){
 $metaCmd=$connection.CreateCommand();$metaCmd.CommandText="SELECT c.name ColumnName,t.name TypeName,c.is_identity FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID('dbo.$table') AND c.is_computed=0 ORDER BY c.column_id"
 $metaAdapter=New-Object System.Data.SqlClient.SqlDataAdapter $metaCmd;$meta=New-Object Data.DataTable;[void]$metaAdapter.Fill($meta)
 $orderColumn=[string]$meta.Rows[0].ColumnName
 $identityColumn=$meta.Rows|Where-Object{$_.is_identity}|Select-Object -First 1
 if($identityColumn){$orderColumn=[string]$identityColumn.ColumnName}
 $dataCmd=$connection.CreateCommand();$dataCmd.CommandText="SELECT * FROM dbo.[$table] ORDER BY [$orderColumn]"
 $dataAdapter=New-Object System.Data.SqlClient.SqlDataAdapter $dataCmd;$data=New-Object Data.DataTable;[void]$dataAdapter.Fill($data)
 [void]$builder.AppendLine();[void]$builder.AppendLine("-- dbo.$table ($($data.Rows.Count) rows)")
 if($data.Rows.Count -eq 0){continue}
 $hasIdentity=@($meta.Rows|Where-Object{$_.is_identity}).Count -gt 0
 if($hasIdentity){[void]$builder.AppendLine("SET IDENTITY_INSERT dbo.[$table] ON;")}
 $columns=($meta.Rows|ForEach-Object{'['+[string]$_.ColumnName+']'}) -join ','
 for($offset=0;$offset -lt $data.Rows.Count;$offset+=500){
  [void]$builder.AppendLine("INSERT dbo.[$table] ($columns) VALUES")
  $limit=[Math]::Min($offset+500,$data.Rows.Count)
  for($i=$offset;$i -lt $limit;$i++){
   $values=@();for($j=0;$j -lt $meta.Rows.Count;$j++){$values+=SqlLiteral $data.Rows[$i][$j] ([string]$meta.Rows[$j].TypeName)}
   $suffix=if($i -eq $limit-1){';'}else{','};[void]$builder.AppendLine('('+($values -join ',')+')'+$suffix)
  }
 }
 if($hasIdentity){[void]$builder.AppendLine("SET IDENTITY_INSERT dbo.[$table] OFF;")}
 [void]$builder.AppendLine('GO')
}
[void]$builder.AppendLine();[void]$builder.AppendLine('-- Monthly report calculation procedure')
[void]$builder.AppendLine($procedure.Trim())
[void]$builder.AppendLine("PRINT 'StudentAttendanceSystem installation completed successfully.';")
$out=(Resolve-Path 'Database').Path+'\AllInOneDatabase.sql'
[IO.File]::WriteAllText($out,$builder.ToString(),[Text.UTF8Encoding]::new($false))
$connection.Close()
Get-Item $out | Select-Object FullName,Length
