# Student Attendance System

A .NET 8 Windows Forms university attendance application backed by SQL Server. Administrators manage lecturers, classes, courses, assignments, students, and timetables. Lecturers take attendance only for assigned scheduled sessions and can review authorized attendance reports.

## Documentation

- [Entity Relationship Diagram](docs/ER-DIAGRAM.md)
- [Application and Attendance Flow Charts](docs/FLOWCHARTS.md)

## Architecture

The application uses logical N-layer separation inside one Visual Studio project:

| Folder | Layer | Responsibility |
|---|---|---|
| `Forms` | Presentation | Login, administrator dashboard, lecturer dashboard, controls, and theme |
| `Services` | Business/application | Authentication, authorization, CRUD, timetable, attendance, and reports |
| `Models` | Domain | User sessions, students, attendance rows, and report records |
| `Data` | Data access | SQL Server connections, commands, and parameterized queries |
| `Database` | Database | Schema, migrations, seed data, and administration scripts |
| `Helpers` | Shared utilities | PBKDF2-SHA256 password hashing |

## Main features

- Administrator and lecturer authentication
- Lecturer activation/deactivation and profile editing
- University classes with department, program, study level, semester 1–10, and academic year
- Course and lecturer-assignment management
- Administrator-managed recurring timetables
- One-hour and two-hour scheduled sessions
- Lecturer attendance limited to assigned sessions
- Present/Absent attendance entry
- Cancelled session handling
- Active/inactive student status
- Home-room lecturer reports
- Monthly weighted attendance percentage
- Light and dark themes using `#027E7E` as the accent color
- Primary-ID ordering and 10-record list pagination

## Requirements

- Windows 10 or newer
- Visual Studio 2022 with the .NET desktop development workload
- .NET 8 SDK
- SQL Server, SQL Server Express, or LocalDB
- SQL Server Management Studio or Visual Studio SQL Server Object Explorer

## Database setup

For a new database, run:

```text
Database/Setup.sql
```

For an older installation, apply the relevant scripts in this order:

1. `Database/MigrateToUniversity.sql`
2. `Database/MigrateSemesterTo10.sql`
3. `Database/MigrateTimetable.sql`
4. `Database/MigrateWeightedAttendance.sql`

Optional project data:

- `Database/ImportCS3BStudents.sql`
- `Database/SeedCS3BTimetable.sql`
- `Database/SeedCT3Timetable.sql`
- `Database/SampleAttendanceJulyAugust2026.sql`

Always back up an existing database before running migration or sample-data scripts.

## Connection string

Configure `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=StudentAttendanceSystem;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

If SQL Server authentication is required:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=StudentAttendanceSystem;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

Do not commit real production usernames or passwords to source control.

## Create the first administrator

After building the project, run this from the solution directory:

```powershell
./StudentAttendanceSystem/Database/BootstrapAdmin.ps1 -Username admin -FullName "System Administrator" -Password (Read-Host -AsSecureString "Password")
```

The script stores a salted PBKDF2-SHA256 hash rather than a plain-text password.

## Build and run

Open `StudentAttendanceSystem.sln` in Visual Studio and press `F5`, or run:

```powershell
dotnet build StudentAttendanceSystem.sln
dotnet run --project StudentAttendanceSystem/StudentAttendanceSystem.csproj
```

## Attendance rules

- Only active users can sign in.
- A lecturer sees only active assignments and scheduled sessions assigned to them.
- The selected class, subject, lecturer, timetable, and student must remain active.
- A one-hour session contributes one attendance unit; a two-hour session contributes two units.
- Present and Late units count as attended.
- Absent units do not count as attended.
- Excused units are removed from the percentage denominator.
- Cancelled sessions are excluded from calculations.
- Attendance below 75% receives a warning remark.

## List behavior

- Management lists are ordered by their primary database ID.
- IDs remain hidden in the user interface.
- Lists show 10 records per page with Previous and Next navigation.
- Student numbers continue across pages: 1–10, 11–20, and so on.

## Security notes

- All database inputs should remain parameterized.
- Password hashes use PBKDF2-SHA256 with a random salt.
- Authorization is checked in services, not only by hiding interface controls.
- Use a restricted SQL Server account for deployment.
- Keep database credentials outside source control for production use.
