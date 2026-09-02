# Student Attendance Management System

Project documentation:

- [StudentAttendanceSystem README](StudentAttendanceSystem/README.md)
- [Entity Relationship Diagram](StudentAttendanceSystem/docs/ER-DIAGRAM.md)
- [System Flow Charts](StudentAttendanceSystem/docs/FLOWCHARTS.md)

This is a .NET 8 Windows Forms application backed by SQL Server. The workspace was empty when implementation began, so there was no prior project architecture or code to preserve.

## Architecture

- `Forms/Auth`, `Forms/Admin`, `Forms/Teacher`, `Forms/Shared`: role-specific dashboards and reusable screens.
- `Models`: session, lookup, attendance, and report types.
- `Services`: authentication, authorization, admin CRUD, students, and attendance/report rules.
- `Data`: centralized parameterized SQL access.
- `Database`: normalized schema, indexes, monthly summary procedure, and secure admin bootstrap.
- `Helpers`: PBKDF2-SHA256 password hashing (100,000 iterations and per-password random salt).

The database contains `Users`, `Teachers`, `Rooms`, `Subjects`, `TeacherAssignments`, `Timetables`, `ClassSessions`, `Students`, `Attendance`, and `MonthlyAttendanceSummary`. Admin defines recurring timetable slots; the application creates dated class sessions as Scheduled, Held, Cancelled, or Rescheduled. Lecturers see only their scheduled sessions for the selected date, and cancelled or pending sessions do not count as student absences. In the university UI, a `Room` is a university class/section and stores Department, Program, Study Level, Semester (1-10 for five-year programs), Academic Year, and academic advisor.

## Setup and run

1. Open `StudentAttendanceSystem.sln` in Visual Studio 2022.
2. Edit `StudentAttendanceSystem/appsettings.json` if `(localdb)\\MSSQLLocalDB` is not the desired SQL Server.
3. Run `StudentAttendanceSystem/Database/Setup.sql` in SQL Server Management Studio or Visual Studio SQL Server Object Explorer.
   If the original school-oriented schema was already installed, run `StudentAttendanceSystem/Database/MigrateToUniversity.sql` instead.
   Existing installations must also run `StudentAttendanceSystem/Database/MigrateTimetable.sql` once.
4. Build the solution once, then create the first admin from PowerShell:

   `./StudentAttendanceSystem/Database/BootstrapAdmin.ps1 -Username admin -FullName "System Administrator" -Password (Read-Host -AsSecureString "Password")`

5. Start the project and log in with that administrator. There is deliberately no registration screen.

## Rules enforced

- Only the admin interface creates teacher accounts; passwords are never stored as plain text.
- Teacher room/subject permissions come from active `TeacherAssignments` and are rechecked in `AuthorizationService` before writes.
- A teacher can manage students only for a subject-assigned room or their home room.
- A teacher records attendance only for an exact assigned room/subject pair.
- A home-room teacher sees all assigned subjects in attendance history, but the entry screen offers only their own subject assignments. The service also rejects writes and cross-teacher overwrites.
- Attendance uniqueness is student + subject + date.
- Monthly reports recalculate whenever requested. Present and Late count as attended, Absent does not, and Excused is excluded from the denominator. Below 75% is marked `Attendance below 75%`; otherwise `Satisfactory`.

## Verification scenarios

For manual verification, create Grade 7A and Grade 7B, Mathematics/English/Science, three teachers, and the assignments described in the specification. Confirm a subject teacher sees only assigned entry combinations; a home-room teacher sees all Grade 7A subjects in history but only their assigned subjects in entry; and Grade 7B remains unavailable without an assignment. Enter 14 attended and 6 absent recorded sessions and refresh the monthly report to verify 70% and the warning remark.
