using Attendly.Application;
using Attendly.Domain;
using Microsoft.Data.SqlClient;
namespace Attendly.Infrastructure;
public sealed class SqlAttendanceRepository(string connectionString):IAttendanceRepository
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request,CancellationToken ct=default)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var q=new SqlCommand("SELECT u.UserId,u.Username,u.PasswordHash,u.FullName,u.Role,t.TeacherId FROM Users u LEFT JOIN Teachers t ON t.UserId=u.UserId WHERE u.Username=@u AND u.IsActive=1",c);q.Parameters.AddWithValue("@u",request.Username.Trim());await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct)||!PasswordHasher.Verify(request.Password,r.GetString(2)))return null;return new(r.GetInt32(0),r.GetString(1),r.GetString(3),r.GetString(4),r.IsDBNull(5)?null:r.GetInt32(5));
    }
    public async Task<PagedResult<Student>> GetStudentsAsync(int roomId,int page=1,int pageSize=10,string? search=null,CancellationToken ct=default)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);search=search?.Trim()??"";await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);const string filter=" FROM Students s JOIN Rooms r ON r.RoomId=s.RoomId WHERE s.RoomId=@r AND (@q='' OR s.StudentCode LIKE '%'+@q+'%' OR s.StudentName LIKE '%'+@q+'%')";await using var count=new SqlCommand("SELECT COUNT(*)"+filter,c);count.Parameters.AddWithValue("@r",roomId);count.Parameters.AddWithValue("@q",search);var total=Convert.ToInt32(await count.ExecuteScalarAsync(ct));await using var cmd=new SqlCommand("SELECT s.StudentId,s.StudentCode,s.StudentName,COALESCE(s.Gender,''),s.RoomId,r.RoomName,s.IsActive"+filter+" ORDER BY s.StudentId OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY",c);cmd.Parameters.AddWithValue("@r",roomId);cmd.Parameters.AddWithValue("@q",search);cmd.Parameters.AddWithValue("@skip",(page-1)*pageSize);cmd.Parameters.AddWithValue("@take",pageSize);await using var rd=await cmd.ExecuteReaderAsync(ct);var items=new List<Student>();while(await rd.ReadAsync(ct))items.Add(new(rd.GetInt32(0),rd.GetString(1),rd.GetString(2),rd.GetString(3),rd.GetInt32(4),rd.GetString(5),rd.GetBoolean(6)));return new(items,page,pageSize,total);
    }
}
