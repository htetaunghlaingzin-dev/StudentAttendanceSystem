SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @RoomId int, @AcademicYear nvarchar(20);
SELECT @RoomId=RoomId,@AcademicYear=AcademicYear FROM Rooms WHERE RoomName=N'CS-3B' AND IsActive=1;
IF @RoomId IS NULL THROW 51000,'Active class CS-3B was not found.',1;
IF (SELECT COUNT(*) FROM Rooms WHERE RoomName=N'CS-3B' AND IsActive=1)<>1 THROW 51001,'More than one active CS-3B class exists.',1;

DECLARE @Students TABLE(StudentCode nvarchar(30),StudentName nvarchar(120));
INSERT @Students VALUES
(N'PaKaPaTa-002627',N'Su Thae Phyu Phyu'),
(N'PaKaPaTa-002628',N'No No Htet'),
(N'PaKaPaTa-002629',N'Khaing Thazin Nwe'),
(N'PaKaPaTa-002631',N'Thiri Thoun'),
(N'PaKaPaTa-002633',N'Zar Kyi Lin'),
(N'PaKaPaTa-002634',N'Thondrai Zaw'),
(N'PaKaPaTa-002639',N'Ei Myat Phyu'),
(N'PaKaPaTa-002640',N'Aye Yadanar Myo'),
(N'PaKaPaTa-002641',N'Thae Thandar Soe'),
(N'PaKaPaTa-002643',N'Daria Thaw Zin'),
(N'PaKaPaTa-002645',N'Jue May Zaw'),
(N'PaKaPaTa-002646',N'Hnin Myo Hlaing'),
(N'PaKaPaTa-002647',N'Kyal Sin Phoo'),
(N'PaKaPaTa-002648',N'Twal Tar Hlaing'),
(N'PaKaPaTa-002650',N'Theint Theint Hanny'),
(N'PaKaPaTa-002651',N'Aung Ko Min'),
(N'PaKaPaTa-002652',N'Phoo Pyae Sone Mon'),
(N'PaKaPaTa-002654',N'Thae Hsu Wai'),
(N'PaKaPaTa-002655',N'Phyo Ko Ko'),
(N'PaKaPaTa-002656',N'Hein Zaw Phyo'),
(N'PaKaPaTa-002658',N'Thet Thet Aung'),
(N'PaKaPaTa-002659',N'Lin Khant Thu'),
(N'PaKaPaTa-002664',N'Hsu Myat Noe'),
(N'PaKaPaTa-002665',N'Sesar Phyu'),
(N'PaKaPaTa-002666',N'Nang Kay Swon Hon'),
(N'PaKaPaTa-002669',N'Hein Htet Soe'),
(N'PaKaPaTa-002671',N'Hnin Yati Lwin'),
(N'PaKaPaTa-002672',N'Sesar'),
(N'PaKaPaTa-002674',N'Moe Set Tun'),
(N'PaKaPaTa-002675',N'Zwe Htet Paing'),
(N'PaKaPaTa-002677',N'Hnin Marlar Aung'),
(N'PaKaPaTa-002678',N'Ei Thinzar'),
(N'PaKaPaTa-002679',N'Zue Zin Htet'),
(N'PaKaPaTa-002680',N'Su Thazin Bo'),
(N'PaKaPaTa-002682',N'Cho Zin Thinzar Myint'),
(N'PaKaPaTa-002683',N'Kaung Sett Han'),
(N'PaKaPaTa-002684',N'Chan Myae San'),
(N'PaKaPaTa-002685',N'Khaing Nyein Thu'),
(N'PaKaPaTa-002689',N'May Tharaphi Win'),
(N'PaKaPaTa-002692',N'Shwe Yee Thin'),
(N'PaKaPaTa-002701',N'Nyi Nyi Zaw'),
(N'PaKaPaTa-002302',N'Thiri Myat Kyaw'),
(N'PaKaPaTa-002445',N'Hsu Shunn Lei Zaw'),
(N'PaKaPaTa-001953',N'Aye Thiri Khaing'),
(N'PaKaPaTa-002234',N'Bhone Khant Min'),
(N'PaKaPaTa-001994',N'Htet Aung Hlaing Zin'),
(N'PaKaPaTa-002889',N'Wai Yan Phyo'),
(N'PaKaPaTa-002890',N'Myint Myat Wai Lwin'),
(N'PaKaPaTa-002891',N'Nu Htet Htet Lwin'),
(N'PaKaPaTa-002892',N'Htet Htet Lwin'),
(N'PaKaPaTa-002893',N'Phoo Pyae Sone'),
(N'PaKaPaTa-002894',N'Kyal Sin Lin Lae'),
(N'PaKaPaTa-002895',N'May Zin Theint'),
(N'PaKaPaTa-002896',N'Kaung Sint Thu'),
(N'PaKaPaTa-002897',N'Pan Momo Aung'),
(N'PaKaPaTa-002103',N'Han Myo Win'),
(N'PaKaPaTa-002477',N'Htet Naing Htut'),
(N'PaKaPaTa-002371',N'Ingyin Khaing'),
(N'PaKaPaTa-003085',N'Khin Phyo Phyo Wai');

IF (SELECT COUNT(*) FROM @Students)<>59 THROW 51002,'The import list must contain exactly 59 students.',1;
IF EXISTS(SELECT StudentCode FROM @Students GROUP BY StudentCode HAVING COUNT(*)>1) THROW 51003,'Duplicate registration number exists in the import list.',1;

UPDATE existing SET StudentName=source.StudentName,RoomId=@RoomId,IsActive=1
FROM Students existing JOIN @Students source ON source.StudentCode=existing.StudentCode AND existing.AcademicYear=@AcademicYear;

INSERT Students(StudentCode,StudentName,Gender,RoomId,AcademicYear,IsActive)
SELECT source.StudentCode,source.StudentName,NULL,@RoomId,@AcademicYear,1
FROM @Students source
WHERE NOT EXISTS(SELECT 1 FROM Students existing WHERE existing.StudentCode=source.StudentCode AND existing.AcademicYear=@AcademicYear);

COMMIT TRANSACTION;
SELECT COUNT(*) AS ImportedStudentCount FROM Students WHERE RoomId=@RoomId AND IsActive=1 AND StudentCode IN(SELECT StudentCode FROM @Students);
