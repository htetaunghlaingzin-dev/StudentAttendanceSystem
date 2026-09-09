USE StudentAttendanceSystem;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ClassSessions',N'U') IS NULL
    THROW 51050, 'Run MigrateTimetable.sql before this backfill.', 1;

DECLARE @Before int=(SELECT COUNT(*) FROM dbo.Attendance WHERE SessionId IS NULL);

DECLARE @Matches TABLE
(
    RoomId int NOT NULL,
    SubjectId int NOT NULL,
    TeacherId int NOT NULL,
    AttendanceDate date NOT NULL,
    TimetableId int NOT NULL,
    PRIMARY KEY(RoomId,SubjectId,TeacherId,AttendanceDate)
);

-- One legacy attendance group represents one class/course/lecturer/date session.
-- If consecutive periods match, use the earliest period because legacy rows did
-- not store a start time and cannot distinguish between those periods.
INSERT @Matches(RoomId,SubjectId,TeacherId,AttendanceDate,TimetableId)
SELECT groups.RoomId,groups.SubjectId,groups.TeacherId,groups.AttendanceDate,match.TimetableId
FROM
(
    SELECT DISTINCT RoomId,SubjectId,TeacherId,AttendanceDate
    FROM dbo.Attendance
    WHERE SessionId IS NULL
) groups
CROSS APPLY
(
    SELECT TOP(1) tt.TimetableId
    FROM dbo.Timetables tt
    WHERE tt.RoomId=groups.RoomId
      AND tt.SubjectId=groups.SubjectId
      AND tt.TeacherId=groups.TeacherId
      AND tt.DayOfWeek=CONVERT(tinyint,(DATEDIFF(day,CONVERT(date,'19000101',112),groups.AttendanceDate)%7)+1)
      AND groups.AttendanceDate BETWEEN tt.EffectiveFrom AND tt.EffectiveTo
    ORDER BY tt.IsActive DESC,tt.StartTime,tt.TimetableId
) match;

BEGIN TRANSACTION;

INSERT dbo.ClassSessions(TimetableId,RoomId,SubjectId,TeacherId,SessionDate,SessionStatus)
SELECT m.TimetableId,m.RoomId,m.SubjectId,m.TeacherId,m.AttendanceDate,'Held'
FROM @Matches m
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ClassSessions cs
    WHERE cs.TimetableId=m.TimetableId AND cs.SessionDate=m.AttendanceDate
);

UPDATE cs SET SessionStatus='Held',UpdatedAt=SYSUTCDATETIME()
FROM dbo.ClassSessions cs
JOIN @Matches m ON m.TimetableId=cs.TimetableId AND m.AttendanceDate=cs.SessionDate
WHERE cs.SessionStatus<>'Held';

UPDATE a SET SessionId=cs.SessionId,UpdatedAt=COALESCE(a.UpdatedAt,SYSUTCDATETIME())
FROM dbo.Attendance a
JOIN @Matches m ON m.RoomId=a.RoomId AND m.SubjectId=a.SubjectId
 AND m.TeacherId=a.TeacherId AND m.AttendanceDate=a.AttendanceDate
JOIN dbo.ClassSessions cs ON cs.TimetableId=m.TimetableId AND cs.SessionDate=m.AttendanceDate
WHERE a.SessionId IS NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Attendance existing
      WHERE existing.SessionId=cs.SessionId AND existing.StudentId=a.StudentId
  );

DECLARE @Updated int=@Before-(SELECT COUNT(*) FROM dbo.Attendance WHERE SessionId IS NULL);
COMMIT TRANSACTION;

SELECT @Before AS NullSessionIdsBefore,
       @Updated AS AttendanceRowsBackfilled,
       COUNT(*) AS NullSessionIdsRemaining
FROM dbo.Attendance
WHERE SessionId IS NULL;

-- Details for records that could not be matched to a real timetable.
SELECT a.RoomId,a.SubjectId,a.TeacherId,a.AttendanceDate,COUNT(*) AS AttendanceRows
FROM dbo.Attendance a
WHERE a.SessionId IS NULL
GROUP BY a.RoomId,a.SubjectId,a.TeacherId,a.AttendanceDate
ORDER BY a.AttendanceDate,a.RoomId,a.SubjectId;
GO
