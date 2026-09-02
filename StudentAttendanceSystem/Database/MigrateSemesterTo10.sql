USE StudentAttendanceSystem;
GO
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.Rooms','Semester') IS NULL
    THROW 51000, 'Run MigrateToUniversity.sql before this migration.', 1;
GO

DECLARE @constraintName sysname;
DECLARE semester_constraints CURSOR LOCAL FAST_FORWARD FOR
SELECT cc.name
FROM sys.check_constraints cc
WHERE cc.parent_object_id=OBJECT_ID('dbo.Rooms')
  AND OBJECT_DEFINITION(cc.object_id) LIKE '%Semester%';

OPEN semester_constraints;
FETCH NEXT FROM semester_constraints INTO @constraintName;
WHILE @@FETCH_STATUS=0
BEGIN
    EXEC(N'ALTER TABLE dbo.Rooms DROP CONSTRAINT '+QUOTENAME(@constraintName));
    FETCH NEXT FROM semester_constraints INTO @constraintName;
END;
CLOSE semester_constraints;
DEALLOCATE semester_constraints;
GO

ALTER TABLE dbo.Rooms WITH CHECK
ADD CONSTRAINT CK_Rooms_Semester CHECK(Semester BETWEEN 1 AND 10);
GO
PRINT 'Semester range updated to 1-10 successfully.';
GO
