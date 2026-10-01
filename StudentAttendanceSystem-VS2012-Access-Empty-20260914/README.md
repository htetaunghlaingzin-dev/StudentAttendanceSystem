# Visual Studio 2012 / .NET Framework 4.5 edition

Open `StudentAttendanceSystem-VS2012.sln` in Visual Studio 2012. This edition uses C# 5, Windows Forms, `System.Data.SqlClient`, and `App.config`; it does not require NuGet packages.

Before running it:

1. Install the .NET Framework 4.5 Developer/Targeting Pack if Visual Studio reports missing reference assemblies.
2. Edit the `DefaultConnection` value in `App.config` when the SQL Server name or password differs.
3. Run the database migrations from the parent project's `Database` directory, including `MigrateCourseYear.sql` and `MigratePresentAbsentOnly.sql`.
4. Build the solution and start it with `F5`.

The legacy interface reproduces the Attendly login and dashboard design with the dark university sidebar, white page header, overview cards, navigation highlights, styled grids and toolbars. It includes administrator lists and activation, course year filtering/editing, student gender selection, lecturer student viewing, timetable viewing, attendance entry, attendance history, monthly reports, and complete Excel-compatible report export.

The Excel export uses Microsoft Spreadsheet XML, which Excel 2003 and newer can open. This avoids an Office installation and third-party packages.
