# Student Attendance System — ER Diagram

`PK` means primary key, `FK` means foreign key, and `UK` means unique key.

```mermaid
erDiagram
    USERS ||--o| TEACHERS : "has profile"
    TEACHERS ||--o{ ROOMS : "home-room lecturer"
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
    CLASS_SESSIONS ||--o{ ATTENDANCE : contains
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
        nvarchar SubjectCode UK
        nvarchar SubjectName
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

## Relationship summary

- `Users` stores authentication and roles; lecturer users have one `Teachers` profile.
- `TeacherAssignments` connects lecturers, classes, and courses for an academic year.
- `Timetables` define recurring teaching slots.
- `ClassSessions` represent dated occurrences of timetable slots.
- `Attendance` connects a session and student with the recorded status.
- `MonthlyAttendanceSummary` stores calculated monthly results for each student and class.

