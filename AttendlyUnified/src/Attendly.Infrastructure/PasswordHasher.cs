using System.Security.Cryptography;
namespace Attendly.Infrastructure;
internal static class PasswordHasher
{
    public static bool Verify(string password,string stored)
    {
        try{var p=stored.Split('$');if(p.Length!=4||p[0]!="PBKDF2-SHA256"||!int.TryParse(p[1],out var n))return false;var salt=Convert.FromBase64String(p[2]);var expected=Convert.FromBase64String(p[3]);var actual=Rfc2898DeriveBytes.Pbkdf2(password,salt,n,HashAlgorithmName.SHA256,expected.Length);return CryptographicOperations.FixedTimeEquals(actual,expected);}catch{return false;}
    }
}
