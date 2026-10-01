using StudentAttendanceSystem.Forms.Admin;
using StudentAttendanceSystem.Forms.Shared;
using StudentAttendanceSystem.Forms.Teacher;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem.Forms.Auth;

public sealed class LoginForm : Form
{
    private readonly AppServices services; private readonly TextBox username = new() { Width = 330, Height = 36 }, password = new() { Width = 330, Height = 36, UseSystemPasswordChar = true }; private readonly Button login;
    public LoginForm(AppServices services)
    {
        this.services = services; Ui.Configure(this, "University Attendance Management System - Login"); Size = new(1080, 680); MinimumSize = new Size(900, 600);
        login = Ui.Button("Sign in", (_, _) => Ui.Safe(LoginAsync)); login.Width = 330; login.Height = 46; AcceptButton = login;
        var left = new Panel { Dock = DockStyle.Left, Width = 430, BackColor = Ui.Primary }; left.Controls.Add(new Label { Text = "●  UCSPyay", Font = new Font("Segoe UI Semibold", 35), ForeColor = Color.White, AutoSize = true, Location = new Point(48, 165) }); left.Controls.Add(new Label { Text = "UNIVERSITY ATTENDANCE PORTAL", Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.FromArgb(151, 158, 191), AutoSize = true, Location = new Point(58, 220) }); left.Controls.Add(new Label { Text = "Secure attendance, clear insights,\nbetter academic outcomes.", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(190, 204, 235), AutoSize = true, Location = new Point(58, 290) });
        var card = new Panel { Size = new Size(440, 420), BackColor = Color.White }; var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(48, 45, 48, 35) }; p.Controls.Add(new Label { Text = "Welcome back", Font = new Font("Segoe UI Semibold", 23), AutoSize = true, ForeColor = Ui.Text, Margin = new Padding(0, 0, 0, 5) }); p.Controls.Add(new Label { Text = "Sign in to continue to your dashboard", Font = new Font("Segoe UI", 10), AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 0, 0, 24) }); p.Controls.Add(Ui.Label("USERNAME")); p.Controls.Add(username); p.Controls.Add(Ui.Label("PASSWORD")); p.Controls.Add(password); p.Controls.Add(new Label { Height = 10 }); p.Controls.Add(login); card.Controls.Add(p);
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Ui.Background }; host.Controls.Add(card); void CenterCard() { card.Left = Math.Max(30, (host.ClientSize.Width - card.Width) / 2); card.Top = Math.Max(30, (host.ClientSize.Height - card.Height) / 2); }
        host.Resize += (_, _) => CenterCard(); Controls.Add(host); Controls.Add(left); CenterCard(); Ui.ThemeContainer(this);
    }
    private async Task LoginAsync() { login.Enabled = false; try { var s = await services.Authentication.LoginAsync(username.Text, password.Text); if (s is null) { MessageBox.Show("Invalid username or password."); return; } Hide(); using Form d = s.Role == Models.UserRole.Admin ? new AdminDashboardForm(services, s) : new TeacherDashboardForm(services, s); d.ShowDialog(); username.Clear(); password.Clear(); Show(); } finally { login.Enabled = true; } }

    private void InitializeComponent()
    {

    }
}
