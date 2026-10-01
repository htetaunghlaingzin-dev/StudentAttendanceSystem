Student Attendance System - Access database package

StudentAttendanceSystem.mdb contains all migrated project data. It can be opened without SQL Server or SQL Express by using Microsoft Access or the 32-bit Microsoft Jet 4.0 provider.

Data: 16 users, 15 teachers, 4 classes, 8 courses, 35 assignments, 59 students, 60 timetable entries, 175 sessions, 9,794 attendance records, and 413 monthly summaries.

The current VS2012 application still contains SQL Server-specific queries. Changing App.config alone will not make its create, edit, attendance, and reporting functions work with this MDB. Those functions require an OleDb/x86 application conversion.
