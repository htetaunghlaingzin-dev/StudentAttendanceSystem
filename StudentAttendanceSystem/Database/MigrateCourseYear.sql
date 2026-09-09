USE StudentAttendanceSystem;
GO
SET XACT_ABORT ON;
GO
IF COL_LENGTH('dbo.Subjects','CourseYear') IS NULL
    ALTER TABLE dbo.Subjects ADD CourseYear tinyint NULL;
GO

-- All courses already stored in this project belong to Third Year.
-- Courses created later receive their year from the Admin course form.
UPDATE dbo.Subjects SET CourseYear=3 WHERE CourseYear IS NULL;
ALTER TABLE dbo.Subjects ALTER COLUMN CourseYear tinyint NOT NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID('dbo.Subjects') AND name='DF_Subjects_CourseYear')
    ALTER TABLE dbo.Subjects ADD CONSTRAINT DF_Subjects_CourseYear DEFAULT 1 FOR CourseYear;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Subjects') AND name='CK_Subjects_CourseYear')
    ALTER TABLE dbo.Subjects ADD CONSTRAINT CK_Subjects_CourseYear CHECK(CourseYear BETWEEN 1 AND 5);
GO
SELECT CourseYear,COUNT(*) CourseCount FROM dbo.Subjects GROUP BY CourseYear ORDER BY CourseYear;
