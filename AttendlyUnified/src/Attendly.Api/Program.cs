using Attendly.Application;
using Attendly.Infrastructure;

var builder=WebApplication.CreateBuilder(args);
var connectionString=builder.Configuration.GetConnectionString("DefaultConnection")??throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");
builder.Services.AddSingleton<IAttendanceRepository>(_=>new SqlAttendanceRepository(connectionString));
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.AllowAnyHeader().AllowAnyMethod().WithOrigins(builder.Configuration["Clients:WebUrl"]??"https://localhost:7202")));
var app=builder.Build();
app.UseHttpsRedirection();app.UseCors();
app.MapGet("/api/health",()=>Results.Ok(new{status="Healthy",service="Attendly API"}));
app.MapPost("/api/auth/login",async(LoginRequest request,IAttendanceRepository repository,CancellationToken ct)=>await repository.LoginAsync(request,ct) is{} user?Results.Ok(user):Results.Unauthorized());
app.MapGet("/api/rooms/{roomId:int}/students",async(int roomId,int? page,int? pageSize,string? search,IAttendanceRepository repository,CancellationToken ct)=>Results.Ok(await repository.GetStudentsAsync(roomId,page??1,pageSize??10,search,ct)));
app.Run();
