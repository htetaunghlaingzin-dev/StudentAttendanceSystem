using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
namespace StudentAttendanceSystemVS2012
{
 sealed class Database
 {
  readonly string cs;
  public string DatabasePath{get;private set;}
  public Database(){DatabasePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"StudentAttendanceSystem.mdb");var configured=ConfigurationManager.ConnectionStrings["DefaultConnection"];var b=new OleDbConnectionStringBuilder(configured==null?"Provider=Microsoft.Jet.OLEDB.4.0;":configured.ConnectionString);b.DataSource=DatabasePath;cs=b.ConnectionString;if(!File.Exists(DatabasePath))throw new FileNotFoundException("StudentAttendanceSystem.mdb was not found beside the application.",DatabasePath);}
  public DataTable Query(string sql,params SqlParameter[] args){using(var c=new OleDbConnection(cs))using(var cmd=Command(c,sql,args)){c.Open();using(var r=cmd.ExecuteReader()){var t=new DataTable();t.Load(r);return t;}}}
  public int Execute(string sql,params SqlParameter[] args){sql=Translate(sql);if(sql.StartsWith("EXEC RecalculateMonthlyAttendance",StringComparison.OrdinalIgnoreCase))return 0;if(sql.StartsWith("MERGE Attendance",StringComparison.OrdinalIgnoreCase))return SaveAttendance(args);using(var c=new OleDbConnection(cs))using(var cmd=Command(c,sql,args)){c.Open();return cmd.ExecuteNonQuery();}}
  int SaveAttendance(SqlParameter[] a){object session=Value(a,"@session"),student=Value(a,"@student");var exists=Query("SELECT AttendanceId FROM Attendance WHERE SessionId=@session AND StudentId=@student",new SqlParameter("@session",session),new SqlParameter("@student",student));if(exists.Rows.Count>0)return Execute("UPDATE Attendance SET [Status]=@status,Remark=@remark,UpdatedAt=Now(),UpdatedBy=@t WHERE AttendanceId=@id",new SqlParameter("@status",Value(a,"@status")),new SqlParameter("@remark",Value(a,"@remark")),new SqlParameter("@t",Value(a,"@t")),new SqlParameter("@id",exists.Rows[0][0]));return Execute("INSERT INTO Attendance(SessionId,StudentId,RoomId,SubjectId,TeacherId,AttendanceDate,[Status],Remark,UpdatedBy,CreatedAt) VALUES(@session,@student,@r,@s,@t,@d,@status,@remark,@t,Now())",new SqlParameter("@session",session),new SqlParameter("@student",student),new SqlParameter("@r",Value(a,"@r")),new SqlParameter("@s",Value(a,"@s")),new SqlParameter("@t",Value(a,"@t")),new SqlParameter("@d",Value(a,"@d")),new SqlParameter("@status",Value(a,"@status")),new SqlParameter("@remark",Value(a,"@remark")));
  }
  int NextId(string table,string key){return Convert.ToInt32(Query("SELECT Nz(MAX(["+key+"]),0)+1 FROM ["+table+"]").Rows[0][0]);}
  static object Value(SqlParameter[] a,string name){foreach(var p in a)if(string.Equals(p.ParameterName,name,StringComparison.OrdinalIgnoreCase))return p.Value??DBNull.Value;return DBNull.Value;}
  static OleDbCommand Command(OleDbConnection c,string sql,SqlParameter[] args){sql=Translate(sql);var cmd=new OleDbCommand();cmd.Connection=c;var matches=Regex.Matches(sql,"@[A-Za-z][A-Za-z0-9_]*");foreach(Match m in matches){var value=Value(args,m.Value);var p=new OleDbParameter();p.ParameterName="?";p.Value=value??DBNull.Value;if(value is string){p.OleDbType=OleDbType.VarChar;p.Size=Math.Max(1,((string)value).Length);}else if(value is DateTime)p.OleDbType=OleDbType.Date;else if(value is TimeSpan){p.OleDbType=OleDbType.Date;p.Value=((DateTime)new DateTime(1899,12,30)).Add((TimeSpan)value);}else if(value is bool)p.OleDbType=OleDbType.Boolean;else if(value is int)p.OleDbType=OleDbType.Integer;cmd.Parameters.Add(p);}cmd.CommandText=Regex.Replace(sql,"@[A-Za-z][A-Za-z0-9_]*","?");return cmd;}
  static string Translate(string s){return s.Replace("SYSUTCDATETIME()","Now()").Replace("GETDATE()","Now()").Replace("ISNULL(","Nz(").Replace("LOWER(","LCase(").Replace("INSERT ","INSERT INTO ").Replace("INSERT INTO INTO ","INSERT INTO ");}
 }
}
