using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
namespace StudentAttendanceSystemVS2012
{
 static class TeacherLoginSmoke
 {
  [STAThread] static void Main(string[] args)
  {
   var db=new Database(args[0]);
   var t=db.Query("SELECT u.UserId,u.Username,u.PasswordHash,u.FullName,u.Role,t.TeacherId FROM Users u LEFT JOIN Teachers t ON t.UserId=u.UserId WHERE u.Username=@u AND u.IsActive=True",new SqlParameter("@u","testteacher"));
   var r=t.Rows[0];
   using(var form=new MainForm(db,Convert.ToInt32(r["UserId"]),Convert.ToInt32(r["TeacherId"]),Convert.ToString(r["Username"]),Convert.ToString(r["FullName"]),Convert.ToString(r["Role"])))
   { var timer=new Timer();timer.Interval=800;timer.Tick+=delegate{timer.Stop();form.Close();};timer.Start();Application.Run(form); }
   Console.WriteLine("TEACHER_LOGIN_OK");
  }
 }
}
