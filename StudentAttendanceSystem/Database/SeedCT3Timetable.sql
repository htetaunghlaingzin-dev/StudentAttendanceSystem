SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @RoomId int;SELECT @RoomId=RoomId FROM Rooms WHERE LOWER(RoomName)=LOWER(N'CT-3') AND IsActive=1;
IF @RoomId IS NULL THROW 51030,'Active CT-3 class was not found.',1;

DECLARE @Slots TABLE(SubjectCode nvarchar(20),DayNo tinyint,StartTime time(0),EndTime time(0));
INSERT @Slots VALUES
(N'CST-3213',1,'10:00','12:00'),(N'CST-3257',1,'13:00','15:00'),
(N'CST-3242',2,'09:00','10:00'),(N'CST-3235',2,'10:00','12:00'),(N'CT-3234',2,'13:00','15:00'),(N'CST-3211',2,'15:00','16:00'),
(N'CST-3242',3,'09:00','11:00'),(N'CST-3256',3,'11:00','12:00'),(N'CST-3256',3,'13:00','14:00'),(N'CST-3235',3,'14:00','16:00'),
(N'CST-3256',4,'09:00','10:00'),(N'CST-3211',4,'10:00','11:00'),(N'CST-3211',4,'11:00','12:00'),(N'CST-3257',4,'13:00','15:00'),(N'CST-3213',4,'15:00','16:00'),
(N'CST-3213',5,'09:00','11:00'),(N'CT-3234',5,'11:00','12:00'),(N'CST-3211',5,'13:00','14:00'),(N'CST-3242',5,'14:00','16:00');

;WITH SourceRows AS
(
 SELECT @RoomId RoomId,s.SubjectId,chosen.TeacherId,x.DayNo,x.StartTime,x.EndTime,r.AcademicYear,r.Semester
 FROM @Slots x JOIN Subjects s ON s.SubjectCode=x.SubjectCode JOIN Rooms r ON r.RoomId=@RoomId
 CROSS APPLY(SELECT TOP(1) ta.TeacherId FROM TeacherAssignments ta JOIN Teachers t ON t.TeacherId=ta.TeacherId JOIN Users u ON u.UserId=t.UserId WHERE ta.RoomId=@RoomId AND ta.SubjectId=s.SubjectId AND ta.IsActive=1 AND u.IsActive=1 ORDER BY ta.AssignmentId) chosen
)
MERGE Timetables target USING SourceRows source
ON target.RoomId=source.RoomId AND target.SubjectId=source.SubjectId AND target.TeacherId=source.TeacherId AND target.DayOfWeek=source.DayNo AND target.StartTime=source.StartTime AND target.AcademicYear=source.AcademicYear AND target.Semester=source.Semester
WHEN MATCHED THEN UPDATE SET EndTime=source.EndTime,EffectiveFrom='2026-07-01',EffectiveTo='2026-12-31',IsActive=1
WHEN NOT MATCHED THEN INSERT(RoomId,SubjectId,TeacherId,DayOfWeek,StartTime,EndTime,AcademicYear,Semester,EffectiveFrom,EffectiveTo) VALUES(source.RoomId,source.SubjectId,source.TeacherId,source.DayNo,source.StartTime,source.EndTime,source.AcademicYear,source.Semester,'2026-07-01','2026-12-31');

SELECT COUNT(*) ActiveCT3TimetableSlots FROM Timetables WHERE RoomId=@RoomId AND IsActive=1;
