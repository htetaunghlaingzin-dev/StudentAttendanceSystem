SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Change these dates if this timetable should apply to a different term.
DECLARE @EffectiveFrom date = '2026-07-01';
DECLARE @EffectiveTo date = '2026-12-31';

DECLARE @CT3RoomId int;
DECLARE @CS3ARoomId int;

SELECT TOP (1) @CT3RoomId = RoomId
FROM Rooms
WHERE REPLACE(UPPER(RoomName), N'-', N'') = N'CT3' AND IsActive = 1
ORDER BY RoomId;
SELECT TOP (1) @CS3ARoomId = RoomId
FROM Rooms
WHERE REPLACE(UPPER(RoomName), N'-', N'') = N'CS3A' AND IsActive = 1
ORDER BY RoomId;

IF @CT3RoomId IS NULL
    THROW 51040, 'Active CT3 or CT-3 class was not found.', 1;
IF @CS3ARoomId IS NULL
    THROW 51041, 'Active CS3A or CS-3A class was not found.', 1;

DECLARE @Slots TABLE
(
    RoomId int NOT NULL,
    ClassName nvarchar(10) NOT NULL,
    SubjectCode nvarchar(20) NOT NULL,
    DayNo tinyint NOT NULL,
    StartTime time(0) NOT NULL,
    EndTime time(0) NOT NULL
);

-- CT3, Room G-3/3. Activity/presentation/discussion periods are intentionally omitted.
INSERT @Slots (RoomId, ClassName, SubjectCode, DayNo, StartTime, EndTime) VALUES
(@CT3RoomId,N'CT3',N'CST-3213',1,'10:00','12:00'),
(@CT3RoomId,N'CT3',N'CST-3257',1,'13:00','15:00'),
(@CT3RoomId,N'CT3',N'CST-3242',2,'09:00','10:00'),
(@CT3RoomId,N'CT3',N'CST-3235',2,'10:00','12:00'),
(@CT3RoomId,N'CT3',N'CT-3234', 2,'13:00','15:00'),
(@CT3RoomId,N'CT3',N'CST-3211',2,'15:00','16:00'),
(@CT3RoomId,N'CT3',N'CST-3242',3,'09:00','11:00'),
(@CT3RoomId,N'CT3',N'CST-3256',3,'11:00','12:00'),
(@CT3RoomId,N'CT3',N'CST-3256',3,'13:00','14:00'),
(@CT3RoomId,N'CT3',N'CST-3235',3,'14:00','16:00'),
(@CT3RoomId,N'CT3',N'CST-3256',4,'09:00','10:00'),
(@CT3RoomId,N'CT3',N'CST-3211',4,'10:00','11:00'),
(@CT3RoomId,N'CT3',N'CST-3211',4,'11:00','12:00'),
(@CT3RoomId,N'CT3',N'CST-3257',4,'13:00','15:00'),
(@CT3RoomId,N'CT3',N'CST-3213',4,'15:00','16:00'),
(@CT3RoomId,N'CT3',N'CST-3213',5,'09:00','11:00'),
(@CT3RoomId,N'CT3',N'CT-3234', 5,'11:00','12:00'),
(@CT3RoomId,N'CT3',N'CST-3211',5,'13:00','14:00'),
(@CT3RoomId,N'CT3',N'CST-3242',5,'14:00','16:00');

-- CS3A, Room G-3/2. Activity/presentation/discussion periods are intentionally omitted.
INSERT @Slots (RoomId, ClassName, SubjectCode, DayNo, StartTime, EndTime) VALUES
(@CS3ARoomId,N'CS3A',N'CS-3224', 1,'09:00','10:00'),
(@CS3ARoomId,N'CS3A',N'CST-3256',1,'11:00','12:00'),
(@CS3ARoomId,N'CS3A',N'CST-3242',1,'13:00','15:00'),
(@CS3ARoomId,N'CS3A',N'CST-3213',1,'15:00','16:00'),
(@CS3ARoomId,N'CS3A',N'CST-3235',2,'09:00','10:00'),
(@CS3ARoomId,N'CS3A',N'CST-3213',2,'10:00','12:00'),
(@CS3ARoomId,N'CS3A',N'CS-3224', 2,'13:00','15:00'),
(@CS3ARoomId,N'CS3A',N'CST-3256',2,'15:00','16:00'),
(@CS3ARoomId,N'CS3A',N'CST-3211',3,'09:00','10:00'),
(@CS3ARoomId,N'CS3A',N'CST-3257',3,'10:00','12:00'),
(@CS3ARoomId,N'CS3A',N'CST-3213',3,'13:00','14:00'),
(@CS3ARoomId,N'CS3A',N'CST-3235',3,'14:00','16:00'),
(@CS3ARoomId,N'CS3A',N'CST-3242',4,'09:00','11:00'),
(@CS3ARoomId,N'CS3A',N'CST-3256',4,'11:00','12:00'),
(@CS3ARoomId,N'CS3A',N'CST-3211',4,'13:00','15:00'),
(@CS3ARoomId,N'CS3A',N'CS-3224', 4,'15:00','16:00'),
(@CS3ARoomId,N'CS3A',N'CST-3257',5,'09:00','11:00'),
(@CS3ARoomId,N'CS3A',N'CST-3211',5,'11:00','12:00'),
(@CS3ARoomId,N'CS3A',N'CST-3235',5,'13:00','14:00'),
(@CS3ARoomId,N'CS3A',N'CST-3242',5,'14:00','15:00');

IF EXISTS
(
    SELECT 1
    FROM @Slots x
    LEFT JOIN Subjects s ON s.SubjectCode = x.SubjectCode AND s.IsActive = 1
    WHERE s.SubjectId IS NULL
)
BEGIN
    DECLARE @MissingSubjects nvarchar(2048);
    SELECT @MissingSubjects = STRING_AGG(SubjectCode, N', ')
    FROM (SELECT DISTINCT x.SubjectCode FROM @Slots x LEFT JOIN Subjects s ON s.SubjectCode=x.SubjectCode AND s.IsActive=1 WHERE s.SubjectId IS NULL) missing;
    DECLARE @MissingSubjectsMessage nvarchar(2048) = CONCAT('Create or activate these courses first: ', @MissingSubjects);
    THROW 51042, @MissingSubjectsMessage, 1;
END;

DECLARE @Resolved TABLE
(
    RoomId int NOT NULL,
    SubjectId int NOT NULL,
    TeacherId int NOT NULL,
    DayNo tinyint NOT NULL,
    StartTime time(0) NOT NULL,
    EndTime time(0) NOT NULL,
    AcademicYear nvarchar(20) NOT NULL,
    Semester tinyint NOT NULL
);

INSERT @Resolved
SELECT x.RoomId, s.SubjectId, chosen.TeacherId, x.DayNo, x.StartTime, x.EndTime,
       r.AcademicYear, r.Semester
FROM @Slots x
JOIN Rooms r ON r.RoomId = x.RoomId
JOIN Subjects s ON s.SubjectCode = x.SubjectCode AND s.IsActive = 1
CROSS APPLY
(
    SELECT TOP (1) ta.TeacherId
    FROM TeacherAssignments ta
    JOIN Teachers t ON t.TeacherId = ta.TeacherId
    JOIN Users u ON u.UserId = t.UserId
    WHERE ta.RoomId = x.RoomId
      AND ta.SubjectId = s.SubjectId
      AND ta.IsActive = 1
      AND u.IsActive = 1
    ORDER BY ta.AssignmentId
) chosen;

IF (SELECT COUNT(*) FROM @Resolved) <> (SELECT COUNT(*) FROM @Slots)
BEGIN
    DECLARE @MissingAssignments nvarchar(2048);
    SELECT @MissingAssignments = STRING_AGG(CONCAT(m.ClassName, N': ', m.SubjectCode), N', ')
    FROM
    (
        SELECT DISTINCT x.ClassName, x.SubjectCode
        FROM @Slots x
        JOIN Subjects s ON s.SubjectCode=x.SubjectCode
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM TeacherAssignments ta
            JOIN Teachers t ON t.TeacherId=ta.TeacherId
            JOIN Users u ON u.UserId=t.UserId
            WHERE ta.RoomId=x.RoomId AND ta.SubjectId=s.SubjectId
              AND ta.IsActive=1 AND u.IsActive=1
        )
    ) m;
    DECLARE @MissingAssignmentsMessage nvarchar(2048) = CONCAT('Create active lecturer assignments first: ', @MissingAssignments);
    THROW 51043, @MissingAssignmentsMessage, 1;
END;

BEGIN TRANSACTION;

MERGE Timetables AS target
USING @Resolved AS source
ON target.RoomId = source.RoomId
AND target.SubjectId = source.SubjectId
AND target.TeacherId = source.TeacherId
AND target.DayOfWeek = source.DayNo
AND target.StartTime = source.StartTime
AND target.AcademicYear = source.AcademicYear
AND target.Semester = source.Semester
WHEN MATCHED THEN UPDATE SET
    EndTime = source.EndTime,
    EffectiveFrom = @EffectiveFrom,
    EffectiveTo = @EffectiveTo,
    IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (RoomId, SubjectId, TeacherId, DayOfWeek, StartTime, EndTime,
            AcademicYear, Semester, EffectiveFrom, EffectiveTo, IsActive)
    VALUES (source.RoomId, source.SubjectId, source.TeacherId, source.DayNo,
            source.StartTime, source.EndTime, source.AcademicYear, source.Semester,
            @EffectiveFrom, @EffectiveTo, 1);

-- Disable old teaching periods for these two classes that are absent from the photos.
UPDATE tt SET IsActive = 0
FROM Timetables tt
JOIN Rooms currentRoom ON currentRoom.RoomId=tt.RoomId
WHERE tt.RoomId IN (@CT3RoomId, @CS3ARoomId)
  AND tt.AcademicYear=currentRoom.AcademicYear
  AND tt.Semester=currentRoom.Semester
  AND NOT EXISTS
  (
      SELECT 1 FROM @Resolved x
      WHERE x.RoomId=tt.RoomId AND x.SubjectId=tt.SubjectId
        AND x.TeacherId=tt.TeacherId AND x.DayNo=tt.DayOfWeek
        AND x.StartTime=tt.StartTime AND x.AcademicYear=tt.AcademicYear
        AND x.Semester=tt.Semester
  );

COMMIT TRANSACTION;

SELECT CASE WHEN RoomId=@CT3RoomId THEN N'CT3' ELSE N'CS3A' END AS ClassName,
       COUNT(*) AS ActiveTimetableSlots
FROM Timetables
WHERE RoomId IN (@CT3RoomId,@CS3ARoomId) AND IsActive=1
GROUP BY RoomId
ORDER BY ClassName;
