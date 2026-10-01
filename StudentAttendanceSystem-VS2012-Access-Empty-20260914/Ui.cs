using System;
using System.Drawing;
using System.Windows.Forms;
namespace StudentAttendanceSystemVS2012
{
 static class Ui
 {
  public static readonly Color Primary=Color.FromArgb(9,39,45),PrimaryLight=Color.FromArgb(13,58,64),Accent=Color.FromArgb(2,126,126),AccentLight=Color.FromArgb(222,245,243),Background=Color.FromArgb(246,247,251),Surface=Color.White,Text=Color.FromArgb(32,37,53),Muted=Color.FromArgb(119,126,145),Border=Color.FromArgb(229,231,239);
  public static Button Button(string text,EventHandler click){var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(110,40),Padding=new Padding(16,7,16,7),BackColor=Accent,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Font=new Font("Segoe UI Semibold",9.5f),Margin=new Padding(6)};b.FlatAppearance.BorderSize=0;b.Click+=click;return b;}
  public static Button Secondary(string text,EventHandler click){var b=Button(text,click);b.BackColor=Color.White;b.ForeColor=Text;b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Border;return b;}
  public static Label Label(string text){return new Label{Text=text,AutoSize=true,Margin=new Padding(8,12,6,6),ForeColor=Muted,Font=new Font("Segoe UI Semibold",9)};}
  public static void Grid(DataGridView g){g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;g.ReadOnly=true;g.AllowUserToAddRows=false;g.AllowUserToDeleteRows=false;g.SelectionMode=DataGridViewSelectionMode.FullRowSelect;g.MultiSelect=false;g.BackgroundColor=Surface;g.BorderStyle=BorderStyle.None;g.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;g.GridColor=Border;g.RowHeadersVisible=false;g.RowTemplate.Height=44;g.EnableHeadersVisualStyles=false;g.ColumnHeadersHeight=46;g.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(248,249,252),ForeColor=Muted,Font=new Font("Segoe UI Semibold",9.5f),SelectionBackColor=Color.FromArgb(248,249,252),Padding=new Padding(8)};g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Surface,ForeColor=Text,SelectionBackColor=AccentLight,SelectionForeColor=Text,Padding=new Padding(8)};g.AlternatingRowsDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(252,252,254)};}
  public static void Style(Control root){root.Font=new Font("Segoe UI",10);foreach(Control c in root.Controls){if(c is TextBox||c is ComboBox||c is DateTimePicker){c.Font=new Font("Segoe UI",10);c.Margin=new Padding(6,7,12,7);}var grid=c as DataGridView;if(grid!=null)Grid(grid);Style(c);}}
 }
}
