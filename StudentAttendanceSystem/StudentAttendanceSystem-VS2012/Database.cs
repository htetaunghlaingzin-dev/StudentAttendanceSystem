using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
namespace StudentAttendanceSystemVS2012
{
 sealed class Database
 {
  readonly string cs=ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
  public DataTable Query(string sql,params SqlParameter[] args){var t=new DataTable();using(var c=new SqlConnection(cs))using(var cmd=new SqlCommand(sql,c)){Normalize(args);cmd.Parameters.AddRange(args);c.Open();using(var r=cmd.ExecuteReader())t.Load(r);}return t;}
  public int Execute(string sql,params SqlParameter[] args){using(var c=new SqlConnection(cs))using(var cmd=new SqlCommand(sql,c)){Normalize(args);cmd.Parameters.AddRange(args);c.Open();return cmd.ExecuteNonQuery();}}
  static void Normalize(SqlParameter[] args){foreach(var parameter in args)if(parameter.Value==null)parameter.Value=DBNull.Value;}
 }
}
