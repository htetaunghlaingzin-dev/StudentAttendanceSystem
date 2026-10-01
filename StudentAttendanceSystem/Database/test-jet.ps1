$cn=New-Object -ComObject ADODB.Connection
$cn.Open('Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\Users\USER\source\repos\StudentAttendanceSystem\StudentAttendanceSystem\Database\provider-test.mdb')
try{$cn.Execute('DROP TABLE T')|Out-Null}catch{}
try{
 $cn.Execute('CREATE TABLE T (Id INTEGER, [Name] TEXT(20))')|Out-Null
 $cn.Execute("INSERT INTO T VALUES(5,'x')")|Out-Null
 $cn.Execute('ALTER TABLE T ALTER COLUMN Id COUNTER')|Out-Null
 'altered'
 $cn.Execute("INSERT INTO T([Name]) VALUES('y')")|Out-Null
 $rs=$cn.Execute('SELECT * FROM T ORDER BY Id')
 while(-not $rs.EOF){"$($rs.Fields(0).Value) $($rs.Fields(1).Value)";$rs.MoveNext()}
}catch{$_.Exception.Message}
$cn.Close()
