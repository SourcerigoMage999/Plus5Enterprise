:setvar ReviewEmail "phase97.review@plus5.local"

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @TeacherId uniqueidentifier = (
    SELECT [Id]
    FROM [UserAccounts]
    WHERE [NormalizedEmail] = UPPER(N'$(ReviewEmail)'));

IF @TeacherId IS NULL
    THROW 51970, 'Create the local Phase 9.7 review account before preparing the fixture.', 1;

DECLARE @SchoolGradeId uniqueidentifier = (
    SELECT TOP (1) [Id]
    FROM [SchoolGrades]
    ORDER BY [SortOrder], [Id]);

IF @SchoolGradeId IS NULL
    THROW 51971, 'Phase 9.7 review requires one SchoolGrade.', 1;

UPDATE [UserAccounts]
SET [Status] = 2,
    [SecurityStamp] = NEWID(),
    [UpdatedAtUtc] = SYSUTCDATETIME()
WHERE [Id] = @TeacherId
  AND [Status] = 1;

DECLARE @ProgramId uniqueidentifier = NEWID();
DECLARE @StudentId uniqueidentifier = NEWID();
DECLARE @Now datetimeoffset(7) = SYSUTCDATETIME();

INSERT INTO [Programs]
    ([Id], [TeacherAccountId], [Name], [NormalizedName], [CreatedAtUtc])
VALUES
    (@ProgramId, @TeacherId, N'Phase 9.7 English', N'PHASE 9.7 ENGLISH', @Now);

INSERT INTO [Students]
    ([Id], [TeacherAccountId], [SchoolGradeId], [ProgramId], [FirstName], [LastName],
     [Nickname], [DateOfBirth], [SchoolName], [Gender], [Email], [Phone],
     [DeliveryMode], [Status], [CreatedAtUtc], [UpdatedAtUtc], [ArchivedAtUtc])
VALUES
    (@StudentId, @TeacherId, @SchoolGradeId, @ProgramId, N'Petar', N'Horvat',
     NULL, NULL, NULL, NULL, NULL, NULL,
     1, 1, @Now, @Now, NULL);

SELECT @TeacherId AS [TeacherId], @StudentId AS [StudentId];
