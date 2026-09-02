# Attendly Unified — Flow Charts

## System architecture

```mermaid
flowchart LR
    W[Web browser] --> WEB[Attendly.Web]
    D[Windows desktop] --> WIN[Attendly.WinForms]
    WEB -->|HTTPS and JSON| API[Attendly.Api]
    WIN -->|HTTPS and JSON| API
    API --> APP[Attendly.Application]
    APP --> DOMAIN[Attendly.Domain]
    API --> INFRA[Attendly.Infrastructure]
    INFRA --> DB[(SQL Server)]
    INFRA --> APP
```

Only the API connects to SQL Server. Database credentials are never stored in the web browser or WinForms client.

## Login flow

```mermaid
flowchart TD
    A[User enters username and password] --> B[Client sends HTTPS login request]
    B --> C{Active username exists?}
    C -- No --> X[Return unauthorized]
    C -- Yes --> D[Verify PBKDF2 password hash]
    D -- Invalid --> X
    D -- Valid --> E[Return user identity and role]
    E --> F{Role}
    F -- Admin --> G[Open administrator dashboard]
    F -- Teacher --> H[Open lecturer dashboard]
```

## Attendance workflow

```mermaid
flowchart TD
    A[Administrator creates lecturer assignment] --> B[Administrator adds timetable]
    B --> C[System generates class session for its date]
    C --> D[Lecturer signs in]
    D --> E[API returns only assigned scheduled sessions]
    E --> F[Lecturer selects a session]
    F --> G[API loads active students ordered by StudentId]
    G --> H[Lecturer marks Present or Absent]
    H --> I{Save confirmed?}
    I -- No --> H
    I -- Yes --> J[API validates lecturer and assignment]
    J --> K[Save attendance in a transaction]
    K --> L[Mark class session Held]
    L --> M[Recalculate monthly attendance]
```

## Attendance calculation

```mermaid
flowchart LR
    A[Held class sessions] --> B[Calculate scheduled duration]
    B --> C{Duration}
    C -- 1 hour --> D[Weight = 1]
    C -- 2 hours --> E[Weight = 2]
    D --> F[Sum status weights]
    E --> F
    F --> G[Present and Late hours]
    F --> H[Total minus Excused hours]
    G --> I[Percentage = attended / eligible × 100]
    H --> I
    I --> J{Below 75 percent?}
    J -- Yes --> K[Attendance warning]
    J -- No --> L[Satisfactory]
```

## Student paging flow

```mermaid
flowchart TD
    A[Choose class or enter Room ID] --> B[Request page number and page size 10]
    B --> C[API filters students]
    C --> D[Order by StudentId]
    D --> E[SQL OFFSET and FETCH 10 rows]
    E --> F[Return items and total count]
    F --> G[Display page and Previous/Next controls]
```

