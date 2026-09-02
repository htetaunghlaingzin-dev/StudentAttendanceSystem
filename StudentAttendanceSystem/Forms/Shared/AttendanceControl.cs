using System.ComponentModel;
using StudentAttendanceSystem.Models;
using StudentAttendanceSystem.Services;

namespace StudentAttendanceSystem.Forms.Shared;

public sealed class AttendanceControl:UserControl
{
    readonly AppServices services;
    readonly UserSession user;
    readonly FlowLayoutPanel assignmentCards=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,AutoScroll=true,Padding=new Padding(12,8,12,8),BackColor=Color.White};
    readonly DateTimePicker date=new(){Width=145,Format=DateTimePickerFormat.Custom,CustomFormat="dd MMM yyyy"};
    readonly DataGridView grid=new(){Dock=DockStyle.Fill};
    readonly Label selectionTitle=new(){Text="Choose an assigned class and course",AutoSize=true,Font=new Font("Segoe UI Semibold",12),ForeColor=Ui.Text,Margin=new Padding(12,14,18,6)};
    BindingList<EditRow> rows=[];
    ScheduledAttendanceSession? selectedAssignment;
    Button? selectedCard;

    public AttendanceControl(AppServices services,UserSession user)
    {
        this.services=services;this.user=user;
        Ui.ConfigureGrid(grid);ConfigureColumns();

        var assignedHeader=new Panel{Dock=DockStyle.Top,Height=66,BackColor=Ui.PrimaryLight,Padding=new Padding(20,8,20,6)};
        var assignedTitle=new Label{Text="MY ASSIGNED SESSIONS",Dock=DockStyle.Top,Height=28,AutoSize=false,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",12.5f),TextAlign=ContentAlignment.MiddleLeft};
        var assignedHint=new Label{Text="Choose one of your scheduled classes below to take attendance",Dock=DockStyle.Fill,AutoSize=false,ForeColor=Color.FromArgb(180,205,208),Font=new Font("Segoe UI",8.5f),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
        assignedHeader.Controls.Add(assignedHint);assignedHeader.Controls.Add(assignedTitle);
        var assignedPanel=new Panel{Dock=DockStyle.Top,Height=178,Padding=new Padding(0,0,0,10),BackColor=Ui.Background};
        assignedPanel.Controls.Add(assignmentCards);assignedPanel.Controls.Add(assignedHeader);

        var toolbar=new Panel{Dock=DockStyle.Top,Height=116,BackColor=Ui.Surface,Padding=new Padding(12,6,12,6)};
        var titleRow=new Panel{Dock=DockStyle.Top,Height=46,BackColor=Ui.Surface};selectionTitle.Dock=DockStyle.Fill;selectionTitle.AutoEllipsis=true;selectionTitle.AutoSize=false;selectionTitle.TextAlign=ContentAlignment.MiddleLeft;titleRow.Controls.Add(selectionTitle);
        var actionRow=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=58,Padding=new Padding(0,3,0,3),BackColor=Ui.Surface,WrapContents=false,AutoScroll=true};actionRow.Controls.Add(Ui.Label("Date"));actionRow.Controls.Add(date);actionRow.Controls.Add(Ui.Button("All present",(_,_)=>SetAll(AttendanceStatus.Present)));actionRow.Controls.Add(Ui.Button("All absent",(_,_)=>SetAll(AttendanceStatus.Absent)));actionRow.Controls.Add(Ui.Button("Save attendance",(_,_)=>Ui.Safe(SaveAsync)));actionRow.Controls.Add(Ui.SecondaryButton("Cancel session",(_,_)=>Ui.Safe(CancelSessionAsync)));toolbar.Controls.Add(actionRow);toolbar.Controls.Add(titleRow);
        var content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(14),BackColor=Ui.Background};content.Controls.Add(grid);content.Controls.Add(toolbar);
        Controls.Add(content);Controls.Add(assignedPanel);

        Load+=(_,_)=>Ui.Safe(InitializeAsync);
        date.ValueChanged+=(_,_)=>Ui.Safe(InitializeAsync);
        grid.DataError+=(_,e)=>{e.ThrowException=false;e.Cancel=true;};
        grid.CellClick+=ChooseAttendanceStatus;
        Ui.ThemeContainer(this);
    }

    async Task InitializeAsync()
    {
        selectedAssignment=null;selectedCard=null;grid.DataSource=null;rows=[];
        var assignments=await services.Attendance.GetScheduledSessionsAsync(user,date.Value.Date);
        assignmentCards.Controls.Clear();
        foreach(var assignment in assignments)
        {
            var card=new Button{Text=$"{assignment.TimeLabel}  •  {assignment.Status}\r\n{assignment.ClassName}\r\n{assignment.CourseName}",Size=new Size(350,88),Margin=new Padding(6),Padding=new Padding(14,7,14,7),TextAlign=ContentAlignment.MiddleLeft,FlatStyle=FlatStyle.Flat,AutoEllipsis=true,BackColor=assignment.Status=="Cancelled"?Color.FromArgb(244,244,246):Color.FromArgb(247,249,253),ForeColor=assignment.Status=="Cancelled"?Ui.Muted:Ui.Text,Font=new Font("Segoe UI Semibold",9),Cursor=Cursors.Hand};
            card.FlatAppearance.BorderColor=Ui.Border;card.FlatAppearance.BorderSize=1;card.FlatAppearance.MouseOverBackColor=Color.FromArgb(235,244,250);
            card.Click+=(_,_)=>Ui.Safe(()=>SelectAssignmentAsync(assignment,card));assignmentCards.Controls.Add(card);
        }
        if(assignments.Count==0){selectionTitle.Text="No scheduled session for this date";grid.DataSource=null;}
        else if(assignmentCards.Controls[0] is Button first)await SelectAssignmentAsync(assignments[0],first);
    }

    async Task SelectAssignmentAsync(ScheduledAttendanceSession assignment,Button card)
    {
        if(selectedCard is not null){selectedCard.BackColor=Color.FromArgb(247,249,253);selectedCard.ForeColor=Ui.Text;selectedCard.FlatAppearance.BorderColor=Ui.Border;selectedCard.FlatAppearance.MouseOverBackColor=Color.FromArgb(235,244,250);}
        selectedAssignment=assignment;selectedCard=card;card.BackColor=Ui.Accent;card.ForeColor=Color.White;card.FlatAppearance.BorderColor=Ui.Accent;card.FlatAppearance.MouseOverBackColor=Color.FromArgb(1,101,101);card.FlatAppearance.MouseDownBackColor=Color.FromArgb(0,82,82);await LoadRowsAsync();
    }

    void ConfigureColumns()
    {
        grid.AutoGenerateColumns=false;grid.ReadOnly=false;grid.Columns.Clear();
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="StudentCode",HeaderText="Registration No.",DataPropertyName=nameof(EditRow.StudentCode),ReadOnly=true,FillWeight=25});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="StudentName",HeaderText="Student Name",DataPropertyName=nameof(EditRow.StudentName),ReadOnly=true,FillWeight=38});
        grid.Columns.Add(new DataGridViewButtonColumn{Name="ChooseStatus",HeaderText="Attendance",DataPropertyName=nameof(EditRow.StatusText),UseColumnTextForButtonValue=false,FlatStyle=FlatStyle.Flat,FillWeight=22});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Remark",HeaderText="Remark",DataPropertyName=nameof(EditRow.Remark),ReadOnly=false,FillWeight=30});
    }

    async Task LoadRowsAsync()
    {
        if(selectedAssignment is not ScheduledAttendanceSession assignment)return;
        selectionTitle.Text=$"{assignment.TimeLabel}  •  {assignment.ClassName}  •  {assignment.CourseName}";
        if(assignment.Status=="Cancelled"){rows=[];grid.DataSource=null;return;}
        var data=await services.Attendance.GetSessionRowsAsync(user,assignment.SessionId);
        rows=new BindingList<EditRow>(data.Select(x=>new EditRow(x)).ToList());
        grid.DataSource=rows;
    }

    void SetAll(AttendanceStatus status)
    {
        grid.EndEdit();foreach(var row in rows)row.Status=status;grid.Refresh();
    }

    void ChooseAttendanceStatus(object? sender,DataGridViewCellEventArgs e)
    {
        if(e.RowIndex<0||grid.Columns[e.ColumnIndex].Name!="ChooseStatus"||grid.Rows[e.RowIndex].DataBoundItem is not EditRow row)return;
        var choice=MessageBox.Show($"Choose attendance for {row.StudentName}:\n\nYes = Present\nNo = Absent\nCancel = Keep current value","Attendance status",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
        if(choice==DialogResult.Yes)row.Status=AttendanceStatus.Present;else if(choice==DialogResult.No)row.Status=AttendanceStatus.Absent;grid.Refresh();
    }

    async Task SaveAsync()
    {
        if(selectedAssignment is not ScheduledAttendanceSession assignment)throw new InvalidOperationException("Choose one of your scheduled sessions first.");
        if(rows.Count==0)throw new InvalidOperationException("There are no active students to save.");
        grid.EndEdit();
        await services.Attendance.SaveSessionAsync(user,assignment.SessionId,rows.Select(x=>new AttendanceRow(x.StudentId,x.StudentCode,x.StudentName,x.Status,x.Remark)));
        MessageBox.Show("Attendance saved successfully.","Saved",MessageBoxButtons.OK,MessageBoxIcon.Information);
    }

    async Task CancelSessionAsync(){if(selectedAssignment is not ScheduledAttendanceSession assignment)throw new InvalidOperationException("Choose a scheduled session first.");if(MessageBox.Show("Cancel this class session? Cancelled sessions are excluded from attendance calculations.","Confirm cancellation",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;await services.Attendance.SetSessionStatusAsync(user,assignment.SessionId,"Cancelled");await InitializeAsync();}

    sealed class EditRow(AttendanceRow source)
    {
        public int StudentId{get;}=source.StudentId;
        public string StudentCode{get;}=source.StudentCode;
        public string StudentName{get;}=source.StudentName;
        public AttendanceStatus Status{get;set;}=source.Status is AttendanceStatus.Present?AttendanceStatus.Present:AttendanceStatus.Absent;
        public string StatusText=>Status==AttendanceStatus.Present?"✓  Present":"✕  Absent";
        public string? Remark{get;set;}=source.Remark;
    }
}
