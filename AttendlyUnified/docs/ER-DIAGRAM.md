# Attendly Unified — Entity Relationship Diagram

This diagram represents the SQL Server schema used by Attendly. `PK` identifies a primary key and `FK` identifies a foreign key.

```mermaid
erDiagram
    USERS ||--o| TEACHERS : "has lecturer profile"
    TEACHERS ||--o{ ROOMS : "is home-room lecturer"
    TEACHERS ||--o{ TEACHER_ASSIGNMENTS : receives
    ROOMS ||--o{ TEACHER_ASSIGNMENTS : has
    SUBJECTS ||--o{ TEACHER_ASSIGNMENTS : covers
    ROOMS ||--o{ STUDENTS : contains
    ROOMS ||--o{ TIMETABLES : schedules
    SUBJECTS ||--o{ TIMETABLES : schedules
    TEACHERS ||--o{ TIMETABLES : teaches
    TIMETABLES ||--o{ CLASS_SESSIONS : generates
    ROOMS ||--o{ CLASS_SESSIONS : hosts
    SUBJECTS ||--o{ CLASS_SESSIONS : covers
    TEACHERS ||--o{ CLASS_SESSIONS : conducts
    CLASS_SESSIONS ||--o{ ATTENDANCE : records
    STUDENTS ||--o{ ATTENDANCE : receives
    ROOMS ||--o{ ATTENDANCE : groups
    SUBJECTS ||--o{ ATTENDANCE : categorizes
    TEACHERS ||--o{ ATTENDANCE : records
    STUDENTS ||--o{ MONTHLY_ATTENDANCE_SUMMARY : summarized
    ROOMS ||--o{ MONTHLY_ATTENDANCE_SUMMARY : summarized

    USERS {
        int UserId PK
        nvarchar Username UK
        nvarchar PasswordHash
        nvarchar FullName
        varchar Role
        bit IsActive
        datetime2 CreatedAt
    }
    TEACHERS {
        int TeacherId PK
        int UserId FK_UK
        nvarchar TeacherCode UK
        nvarchar FullName
        nvarchar Phone
    }
    ROOMS {
        int RoomId PK
        nvarchar RoomName
        nvarchar Department
        nvarchar ProgramName
        nvarchar StudyLevel
        tinyint Semester
        nvarchar AcademicYear
        int HomeRoomTeacherId FK
        bit IsActive
    }
    SUBJECTS {
        int SubjectId PK
        nvarchar SubjectName
        nvarchar SubjectCode UK
        bit IsActive
    }
    TEACHER_ASSIGNMENTS {
        int AssignmentId PK
        int TeacherId FK
        int RoomId FK
        int SubjectId FK
        nvarchar AcademicYear
        bit IsActive
    }
    STUDENTS {
        int StudentId PK
        nvarchar StudentCode
        nvarchar StudentName
        nvarchar Gender
        int RoomId FK
        nvarchar AcademicYear
        bit IsActive
    }
    TIMETABLES {
        int TimetableId PK
        int RoomId FK
        int SubjectId FK
        int TeacherId FK
        tinyint DayOfWeek
        time StartTime
        time EndTime
        nvarchar AcademicYear
        tinyint Semester
        date EffectiveFrom
        date EffectiveTo
        bit IsActive
    }
    CLASS_SESSIONS {
        int SessionId PK
        int TimetableId FK
        int RoomId FK
        int SubjectId FK
        int TeacherId FK
        date SessionDate
        varchar SessionStatus
        datetime2 CreatedAt
        datetime2 UpdatedAt
    }
    ATTENDANCE {
        int AttendanceId PK
        int SessionId FK
        int StudentId FK
        int RoomId FK
        int SubjectId FK
        int TeacherId FK
        date AttendanceDate
        varchar Status
        nvarchar Remark
        int UpdatedBy FK
    }
    MONTHLY_ATTENDANCE_SUMMARY {
        int SummaryId PK
        int StudentId FK
        int RoomId FK
        tinyint Month
        smallint Year
        int TotalSessions
        int PresentCount
        int AbsentCount
        int LateCount
        int ExcusedCount
        decimal AttendancePercentage
        nvarchar Remark
    }
```

## Important rules

- A user may have one lecturer profile; administrator users do not require one.
- A class may have one home-room lecturer.
- A lecturer assignment is unique by lecturer, class, subject, and academic year.
- A timetable entry generates dated class sessions.
- Attendance is unique for each student within a generated session.
- Semesters are limited to 1–10.
- Inactive users, classes, subjects, assignments, timetables, and students remain stored for historical reporting.

