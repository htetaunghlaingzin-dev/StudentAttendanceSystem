param(
 [string]$DatabasePath = '',
 [string]$Username = 'admin',
 [Parameter(Mandatory=$true)][Security.SecureString]$NewPassword
)
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($DatabasePath)){
 $debugCopy=Join-Path $PSScriptRoot 'bin\Debug\StudentAttendanceSystem.mdb'
 $DatabasePath=if(Test-Path -LiteralPath $debugCopy){$debugCopy}else{Join-Path $PSScriptRoot 'StudentAttendanceSystem.mdb'}
}
if(-not (Test-Path -LiteralPath $DatabasePath)){throw "Database not found: $DatabasePath"}
Add-Type -AssemblyName System.Data
Add-Type @'
using System;
using System.Security.Cryptography;
using System.Text;
public static class AccessPasswordHash {
 public static string Hash(string password){byte[] salt=new byte[16];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(salt);byte[] hash=Pbkdf2(password,salt,100000,32);return "PBKDF2-SHA256$100000$"+Convert.ToBase64String(salt)+"$"+Convert.ToBase64String(hash);}
 static byte[] Pbkdf2(string password,byte[] salt,int iterations,int length){using(var h=new HMACSHA256(Encoding.UTF8.GetBytes(password))){byte[] result=new byte[length];int block=1,offset=0;while(offset<length){byte[] input=new byte[salt.Length+4];Buffer.BlockCopy(salt,0,input,0,salt.Length);input[input.Length-4]=(byte)(block>>24);input[input.Length-3]=(byte)(block>>16);input[input.Length-2]=(byte)(block>>8);input[input.Length-1]=(byte)block;byte[] u=h.ComputeHash(input),f=(byte[])u.Clone();for(int i=1;i<iterations;i++){u=h.ComputeHash(u);for(int j=0;j<f.Length;j++)f[j]^=u[j];}int count=Math.Min(f.Length,length-offset);Buffer.BlockCopy(f,0,result,offset,count);offset+=count;block++;}return result;}}
}
'@
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($NewPassword)
try{
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
 if($plain.Length -lt 8){throw 'The new password must contain at least 8 characters.'}
 $connection=New-Object Data.OleDb.OleDbConnection "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$([IO.Path]::GetFullPath($DatabasePath));"
 $command=$connection.CreateCommand();$command.CommandText='UPDATE Users SET PasswordHash=? WHERE Username=? AND IsActive=True'
 [void]$command.Parameters.Add('@hash',[Data.OleDb.OleDbType]::LongVarWChar,1000);$command.Parameters[0].Value=[AccessPasswordHash]::Hash($plain)
 [void]$command.Parameters.Add('@username',[Data.OleDb.OleDbType]::VarChar,50);$command.Parameters[1].Value=$Username
 $connection.Open();$changed=$command.ExecuteNonQuery();$connection.Close()
 if($changed -ne 1){throw "Active user '$Username' was not found."}
 Write-Host "Password reset successfully for $Username." -ForegroundColor Green
 Write-Host "Database: $([IO.Path]::GetFullPath($DatabasePath))"
} finally {if($pointer -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)};$plain=$null}
