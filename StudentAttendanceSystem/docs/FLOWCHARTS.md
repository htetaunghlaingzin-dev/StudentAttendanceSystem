# Student Attendance System — Flow Charts

## WinForms architecture

```mermaid
flowchart LR
    USER[Administrator or Lecturer] --> UI[Windows Forms presentation]
    UI --> SERVICES[Business and authorization services]
    SERVICES --> MODELS[Domain models]
    SERVICES --> DATA[Centralized data access]
    DATA --> DB[(SQL Server)]
    DB --> SCRIPTS[Schema, migrations and procedures]
```

## Login and role selection

```mermaid
flowchart TD
    A[Enter username and password] --> B[Authentication service finds active user]
    B --> C{User found?}
    C -- No --> X[Show invalid-login message]
    C -- Yes --> D[Verify PBKDF2-SHA256 hash]
    D -- Invalid --> X
    D -- Valid --> E{Role}
    E -- Admin --> F[Open administrator dashboard]
    E -- Teacher --> G[Open lecturer dashboard]
```

## Administration workflow

```mermaid
flowchart TD
    A[Create active lecturer] --> B[Create university class]
    B --> C[Create course]
    C --> D[Assign lecturer to class and course]
    D --> E[Add timetable slot]
    E --> F[Add or import students]
    F --> G[System is ready to generate sessions]
```

## Take-attendance workflow

```mermaid
flowchart TD
    A[Lecturer chooses date] --> B[Load active timetable sessions]
    B --> C[Show only lecturer assignments]
    C --> D{Select scheduled session}
    D --> E[Load active students ordered by StudentId]
    E --> F[Mark each student Present or Absent]
    F --> G{Save attendance?}
    G -- No --> F
    G -- Yes --> H[Recheck lecturer authorization]
    H --> I{Authorized and records valid?}
    I -- No --> J[Show warning and do not save]
    I -- Yes --> K[Save all rows in SQL transaction]
    K --> L[Mark class session Held]
```

## Timetable and session lifecycle

```mermaid
stateDiagram-v2
    [*] --> Scheduled: timetable creates dated session
    Scheduled --> Held: attendance saved
    Scheduled --> Cancelled: lecturer cancels session
    Scheduled --> Rescheduled: schedule changes
    Rescheduled --> Held: attendance saved on replacement
    Cancelled --> [*]
    Held --> [*]
```

## Weighted attendance calculation

```mermaid
flowchart TD
    A[Read held sessions for month] --> B[Calculate each timetable duration]
    B --> C{Session duration}
    C -- One hour --> D[Attendance weight 1]
    C -- Two hours --> E[Attendance weight 2]
    D --> F[Sum Present, Absent, Late and Excused weights]
    E --> F
    F --> G[Eligible = total minus excused]
    G --> H[Attended = present plus late]
    H --> I[Percentage = attended divided by eligible × 100]
    I --> J{Percentage below 75?}
    J -- Yes --> K[Attendance below 75 percent]
    J -- No --> L[Satisfactory]
```

## Ten-record list paging

```mermaid
flowchart LR
    A[Load or search records] --> B[Order by primary ID]
    B --> C[Calculate total pages]
    C --> D[Skip previous page rows]
    D --> E[Take 10 rows]
    E --> F[Display continuous No. values]
    F --> G[Previous or Next]
    G --> D
```

## Status update workflow

```mermaid
flowchart TD
    A[Select lecturer or student] --> B{Currently active?}
    B -- Yes --> C[Confirm deactivation or withdrawal]
    B -- No --> D[Confirm reactivation]
    C --> E[Update IsActive]
    D --> E
    E --> F[Keep historical attendance records]
    F --> G[Refresh paged list]
```
