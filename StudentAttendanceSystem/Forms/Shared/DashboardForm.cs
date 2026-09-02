using StudentAttendanceSystem.Models;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem.Forms.Shared;

public abstract class DashboardForm:Form
{
    protected AppServices Services{get;} protected UserSession Session{get;}
    readonly Panel navigation=new(){Dock=DockStyle.Left,Width=230,BackColor=Ui.Primary};
    readonly Panel navigationMenu=new(){Dock=DockStyle.Fill,BackColor=Ui.Primary,AutoScroll=true};
    readonly Panel contentHost=new(){Dock=DockStyle.Fill,BackColor=Ui.Background,Padding=new Padding(24,20,24,24)};
    readonly Label pageTitle=new(){AutoSize=true,Font=new Font("Segoe UI Semibold",18),ForeColor=Ui.Text,Location=new Point(26,15)};
    readonly List<Button> navButtons=[]; Control? current;
    protected DashboardForm(AppServices services,UserSession session,string role)
    {
        Services=services;Session=session;Ui.Configure(this,$"{role} Dashboard - {session.FullName}");WindowState=FormWindowState.Maximized;
        var brand=new Panel{Dock=DockStyle.Fill,BackColor=Ui.Primary};brand.Controls.Add(new Label{Text="●",ForeColor=Ui.Accent,Font=new Font("Segoe UI Semibold",17),AutoSize=true,Location=new Point(19,24)});brand.Controls.Add(new Label{Text="Attendly",ForeColor=Color.White,Font=new Font("Segoe UI Semibold",20),AutoSize=true,Location=new Point(48,20)});brand.Controls.Add(new Label{Text="UNIVERSITY ATTENDANCE PORTAL",ForeColor=Color.FromArgb(151,175,180),Font=new Font("Segoe UI Semibold",7.5f),AutoSize=true,Location=new Point(22,63)});
        var profile=new Panel{Dock=DockStyle.Fill,BackColor=Ui.PrimaryLight};profile.Controls.Add(new Label{Text=session.FullName,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10.5f),AutoEllipsis=true,AutoSize=false,Size=new Size(190,25),Location=new Point(20,14)});profile.Controls.Add(new Label{Text=$"{role}  •  @{session.Username}",ForeColor=Color.FromArgb(167,190,194),AutoEllipsis=true,AutoSize=false,Size=new Size(190,22),Location=new Point(20,43)});var logout=new Button{Text="Sign out",Size=new Size(190,36),Location=new Point(20,78),FlatStyle=FlatStyle.Flat,BackColor=Color.Transparent,ForeColor=Color.White,Cursor=Cursors.Hand};logout.FlatAppearance.BorderColor=Color.FromArgb(75,105,110);logout.Click+=(_,_)=>Close();profile.Controls.Add(logout);
        var sidebarLayout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,BackColor=Ui.Primary,Margin=Padding.Empty};sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,104));sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,128));sidebarLayout.Controls.Add(brand,0,0);sidebarLayout.Controls.Add(navigationMenu,0,1);sidebarLayout.Controls.Add(profile,0,2);navigation.Controls.Add(sidebarLayout);
        var header=new Panel{Dock=DockStyle.Top,Height=64,BackColor=Ui.Surface};var today=new Label{Text=DateTime.Today.ToString("dddd, dd MMMM yyyy"),AutoSize=true,ForeColor=Ui.Muted,Font=new Font("Segoe UI",9.5f),Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(760,22)};var theme=Ui.SecondaryButton("Dark mode",(_,_)=>{});theme.Click+=(_,_)=>ToggleTheme();theme.AutoSize=false;theme.Size=new Size(105,36);theme.Anchor=AnchorStyles.Top|AnchorStyles.Right;theme.Location=new Point(900,13);header.Controls.Add(pageTitle);header.Controls.Add(today);header.Controls.Add(theme);header.Resize+=(_,_)=>{theme.Left=header.ClientSize.Width-theme.Width-22;today.Left=theme.Left-today.Width-24;};
        Controls.Add(contentHost);Controls.Add(header);Controls.Add(navigation);
        void ToggleTheme(){Ui.SetDarkMode(!Ui.IsDark);theme.Text=Ui.IsDark?"Light mode":"Dark mode";Ui.ApplyTheme(this);navigation.BackColor=Ui.Primary;brand.BackColor=Ui.Primary;profile.BackColor=Ui.PrimaryLight;header.BackColor=Ui.Surface;contentHost.BackColor=Ui.Background;foreach(var b in navButtons)if(b.BackColor!=Ui.Accent)b.BackColor=Ui.Primary;Invalidate(true);}
    }
    protected void AddTab(string title,Control content)
    {
        content.Dock=DockStyle.Fill;content.BackColor=Ui.Background;Ui.ThemeContainer(content);
        var button=new Button{Text="   "+title,TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Top,Height=48,FlatStyle=FlatStyle.Flat,BackColor=Ui.Primary,ForeColor=Color.FromArgb(188,208,211),Font=new Font("Segoe UI Semibold",9.5f),Cursor=Cursors.Hand,Padding=new Padding(10,0,0,0)};button.FlatAppearance.BorderSize=0;button.FlatAppearance.MouseOverBackColor=Ui.PrimaryLight;
        button.Click+=(_,_)=>ShowPage(title,content,button);navigationMenu.Controls.Add(button);navigationMenu.Controls.SetChildIndex(button,0);navButtons.Add(button);if(current is null)ShowPage(title,content,button);
    }
    void ShowPage(string title,Control content,Button selected){contentHost.SuspendLayout();contentHost.Controls.Clear();contentHost.Controls.Add(content);contentHost.ResumeLayout();current=content;pageTitle.Text=title;foreach(var b in navButtons){b.BackColor=b==selected?Ui.Accent:Ui.Primary;b.ForeColor=Color.White;}}
}
