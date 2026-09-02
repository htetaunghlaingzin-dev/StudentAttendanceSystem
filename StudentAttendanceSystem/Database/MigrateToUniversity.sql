USE StudentAttendanceSystem;
GO
SET XACT_ABORT ON;
GO

-- Each schema change is in its own batch because SQL Server compiles column
-- references before executing conditional ALTER TABLE statements.
IF COL_LENGTH('dbo.Rooms','Department') IS NULL
    ALTER TABLE dbo.Rooms ADD Department nvarchar(100) NULL;
GO
UPDATE dbo.Rooms SET Department='General' WHERE Department IS NULL OR LTRIM(RTRIM(Department))='';
ALTER TABLE dbo.Rooms ALTER COLUMN Department nvarchar(100) NOT NULL;
GO

IF COL_LENGTH('dbo.Rooms','ProgramName') IS NULL
    ALTER TABLE dbo.Rooms ADD ProgramName nvarchar(120) NULL;
GO
UPDATE dbo.Rooms SET ProgramName='General Program' WHERE ProgramName IS NULL OR LTRIM(RTRIM(ProgramName))='';
ALTER TABLE dbo.Rooms ALTER COLUMN ProgramName nvarchar(120) NOT NULL;
GO

IF COL_LENGTH('dbo.Rooms','StudyLevel') IS NULL
    ALTER TABLE dbo.Rooms ADD StudyLevel nvarchar(30) NULL;
GO
IF COL_LENGTH('dbo.Rooms','Grade') IS NOT NULL
    EXEC(N'UPDATE dbo.Rooms SET StudyLevel=Grade WHERE StudyLevel IS NULL');
UPDATE dbo.Rooms SET StudyLevel='Year 1' WHERE StudyLevel IS NULL OR LTRIM(RTRIM(StudyLevel))='';
ALTER TABLE dbo.Rooms ALTER COLUMN StudyLevel nvarchar(30) NOT NULL;
GO

IF COL_LENGTH('dbo.Rooms','Semester') IS NULL
    ALTER TABLE dbo.Rooms ADD Semester tinyint NULL;
GO
UPDATE dbo.Rooms SET Semester=1 WHERE Semester IS NULL OR Semester NOT BETWEEN 1 AND 10;
ALTER TABLE dbo.Rooms ALTER COLUMN Semester tinyint NOT NULL;
GO

DECLARE @semesterCheck sysname;
SELECT TOP (1) @semesterCheck=cc.name
FROM sys.check_constraints cc
JOIN sys.columns c ON c.object_id=cc.parent_object_id AND c.column_id=cc.parent_column_id
WHERE cc.parent_object_id=OBJECT_ID('dbo.Rooms') AND c.name='Semester';
IF @semesterCheck IS NOT NULL EXEC(N'ALTER TABLE dbo.Rooms DROP CONSTRAINT '+QUOTENAME(@semesterCheck));
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Rooms_Semester' AND parent_object_id=OBJECT_ID('dbo.Rooms'))
    ALTER TABLE dbo.Rooms ADD CONSTRAINT CK_Rooms_Semester CHECK(Semester BETWEEN 1 AND 10);
GO

IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name='UQ_Room' AND parent_object_id=OBJECT_ID('dbo.Rooms'))
    ALTER TABLE dbo.Rooms DROP CONSTRAINT UQ_Room;
GO
IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name='UQ_Room' AND parent_object_id=OBJECT_ID('dbo.Rooms'))
    ALTER TABLE dbo.Rooms ADD CONSTRAINT UQ_Room UNIQUE(ProgramName,RoomName,AcademicYear,Semester);
GO

IF COL_LENGTH('dbo.Rooms','Grade') IS NOT NULL
    ALTER TABLE dbo.Rooms DROP COLUMN Grade;
GO

PRINT 'University schema migration completed successfully.';
GO
