using Attendly.Domain;

namespace Attendly.Application;

public sealed record LoginRequest(string Username,string Password);
public sealed record LoginResponse(int UserId,string Username,string FullName,string Role,int? TeacherId);
public sealed record PagedResult<T>(IReadOnlyList<T> Items,int Page,int PageSize,int TotalCount)
{
    public int TotalPages=>Math.Max(1,(int)Math.Ceiling(TotalCount/(double)PageSize));
}
public interface IAttendanceRepository
{
    Task<LoginResponse?> LoginAsync(LoginRequest request,CancellationToken cancellationToken=default);
    Task<PagedResult<Student>> GetStudentsAsync(int roomId,int page=1,int pageSize=10,string? search=null,CancellationToken cancellationToken=default);
}
