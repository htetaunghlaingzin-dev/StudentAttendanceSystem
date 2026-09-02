using Microsoft.Data.SqlClient;
using StudentAttendanceSystem.Data;
using StudentAttendanceSystem.Helpers;
using StudentAttendanceSystem.Models;

namespace StudentAttendanceSystem.Services;

public sealed class AuthenticationService(Database db)
{
    public async Task<UserSession?> LoginAsync(string username, string password)
    {
        const string sql = "SELECT u.UserId,u.Username,u.PasswordHash,u.FullName,u.Role,t.TeacherId FROM Users u LEFT JOIN Teachers t ON t.UserId=u.UserId WHERE u.Username=@u AND u.IsActive=1";
        var rows = await db.QueryAsync(sql, r => new { Id=r.GetInt32(0), Username=r.GetString(1), Hash=r.GetString(2), Name=r.GetString(3), Role=r.GetString(4), TeacherId=r.IsDBNull(5)?(int?)null:r.GetInt32(5) }, new SqlParameter("@u", username.Trim()));
        var user = rows.SingleOrDefault();
        if (user is null || !PasswordHasher.Verify(password, user.Hash)) return null;
        return new UserSession(user.Id, user.TeacherId, user.Username, user.Name, Enum.Parse<UserRole>(user.Role));
    }
}
