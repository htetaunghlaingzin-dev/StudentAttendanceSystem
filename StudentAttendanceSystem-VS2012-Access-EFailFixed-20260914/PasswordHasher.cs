using System;
using System.Security.Cryptography;
namespace StudentAttendanceSystemVS2012
{
 static class PasswordHasher
 {
  public static string Hash(string password){var salt=new byte[16];using(var random=RandomNumberGenerator.Create())random.GetBytes(salt);var hash=Pbkdf2(password,salt,100000,32);return "PBKDF2-SHA256$100000$"+Convert.ToBase64String(salt)+"$"+Convert.ToBase64String(hash);}
  public static bool Verify(string password,string stored){try{var p=stored.Split('$');if(p.Length!=4||p[0]!="PBKDF2-SHA256")return false;var expected=Convert.FromBase64String(p[3]);var actual=Pbkdf2(password,Convert.FromBase64String(p[2]),int.Parse(p[1]),expected.Length);var diff=actual.Length^expected.Length;for(var i=0;i<actual.Length&&i<expected.Length;i++)diff|=actual[i]^expected[i];return diff==0;}catch{return false;}}
  static byte[] Pbkdf2(string password,byte[] salt,int iterations,int length){using(var h=new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(password))){var result=new byte[length];var block=1;var offset=0;while(offset<length){var input=new byte[salt.Length+4];Buffer.BlockCopy(salt,0,input,0,salt.Length);input[input.Length-4]=(byte)(block>>24);input[input.Length-3]=(byte)(block>>16);input[input.Length-2]=(byte)(block>>8);input[input.Length-1]=(byte)block;var u=h.ComputeHash(input);var f=(byte[])u.Clone();for(var i=1;i<iterations;i++){u=h.ComputeHash(u);for(var j=0;j<f.Length;j++)f[j]^=u[j];}var count=Math.Min(f.Length,length-offset);Buffer.BlockCopy(f,0,result,offset,count);offset+=count;block++;}return result;}}
 }
}
