:setvar ReviewEmail "phase65.review@plus5.local"

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @OwnerId uniqueidentifier = (
    SELECT [Id]
    FROM [UserAccounts]
    WHERE [NormalizedEmail] = UPPER(N'$(ReviewEmail)'));

IF @OwnerId IS NULL
    THROW 51650, 'Create the local Phase 6.5 review account before preparing the fixture.', 1;

UPDATE [UserAccounts]
SET [Status] = 2,
    [SecurityStamp] = NEWID(),
    [UpdatedAtUtc] = SYSUTCDATETIME()
WHERE [Id] = @OwnerId
  AND [Status] = 1;

DECLARE @MaterialId uniqueidentifier = '65000000-0000-0000-0100-000000000001';
DECLARE @MaterialVersionId uniqueidentifier = '65000000-0000-0000-0200-000000000001';
DECLARE @FileId uniqueidentifier = '65000000-0000-0000-0300-000000000001';
DECLARE @FirstTaskId uniqueidentifier = '65000000-0000-0000-0400-000000000001';
DECLARE @SecondTaskId uniqueidentifier = '65000000-0000-0000-0400-000000000002';
DECLARE @FirstTaskVersionId uniqueidentifier = '65000000-0000-0000-0500-000000000001';
DECLARE @SecondTaskVersionId uniqueidentifier = '65000000-0000-0000-0500-000000000002';
DECLARE @KnowledgeComponentId uniqueidentifier = '56000000-0000-0000-0000-000000000220';
DECLARE @CreatedAt datetimeoffset(7) = '2026-10-04T12:00:00+00:00';

IF NOT EXISTS (SELECT 1 FROM [Materials] WHERE [Id] = @MaterialId)
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO [Materials]
        ([Id], [OwnerTeacherId], [Status], [Visibility], [CurrentVersionId],
         [CreatedAtUtc], [UpdatedAtUtc], [ArchivedAtUtc])
    VALUES
        (@MaterialId, @OwnerId, 1, 1, NULL, @CreatedAt, @CreatedAt, NULL);

    INSERT INTO [MaterialVersions]
        ([Id], [MaterialId], [VersionNumber], [CreatedByTeacherId], [Title], [Description],
         [MaterialTypeCode], [Subject], [LanguageCode], [ProgramId], [SchoolGradeId],
         [ProficiencyLevelId], [LearningGoal], [Status], [CreatedAtUtc], [ActivatedAtUtc],
         [SupersededAtUtc])
    VALUES
        (@MaterialVersionId, @MaterialId, 1, @OwnerId,
         N'Present Perfect – procjenjivi zadaci',
         N'Radni list s dva strukturirana zadatka za provjeru razumijevanja i primjene.',
         N'WORKSHEET', N'Engleski jezik', N'hr-HR', NULL, NULL, NULL,
         N'Učenik prepoznaje i primjenjuje Present Perfect u kratkom kontekstu.',
         1, @CreatedAt, NULL, NULL);

    INSERT INTO [MaterialFiles]
        ([Id], [MaterialVersionId], [Format], [OriginalFileName], [DeclaredMediaType],
         [DeclaredSizeBytes], [ActualSizeBytes], [StorageProvider], [StorageContainer],
         [ObjectKey], [Sha256Checksum], [Status], [ScanAttemptCount],
         [LastScanResultCategory], [CreatedAtUtc], [UpdatedAtUtc], [UploadedAtUtc],
         [ScannedAtUtc])
    VALUES
        (@FileId, @MaterialVersionId, 1, N'present-perfect-procjena.pdf', N'application/pdf',
         4096, 4096, N'R2', N'visual-fixtures',
         N'materials/phase65/review/present-perfect-procjena.pdf',
         REPLICATE('A', 64), 4, 1, N'CLEAN', @CreatedAt, DATEADD(minute, 3, @CreatedAt),
         DATEADD(minute, 1, @CreatedAt), DATEADD(minute, 3, @CreatedAt));

    INSERT INTO [MaterialVersionTags] ([MaterialVersionId], [NormalizedName], [Name])
    VALUES (@MaterialVersionId, N'PRESENT PERFECT', N'present perfect');

    INSERT INTO [MaterialVersionKnowledgeComponents]
        ([MaterialVersionId], [KnowledgeComponentId])
    VALUES (@MaterialVersionId, @KnowledgeComponentId);

    INSERT INTO [AssessableTasks] ([Id], [MaterialId], [CreatedAtUtc])
    VALUES
        (@FirstTaskId, @MaterialId, @CreatedAt),
        (@SecondTaskId, @MaterialId, @CreatedAt);

    INSERT INTO [AssessableTaskVersions]
        ([Id], [MaterialId], [AssessableTaskId], [MaterialVersionId], [VersionNumber],
         [SortOrder], [Prompt], [TaskTypeCode], [Difficulty], [EvidenceType],
         [CorrectAnswer], [EvaluationCriterion], [MaxPoints], [CreatedAtUtc])
    VALUES
        (@FirstTaskVersionId, @MaterialId, @FirstTaskId, @MaterialVersionId, 1, 0,
         N'I ____ London twice.', N'SINGLE_CHOICE', 1, 1,
         N'B – have visited', NULL, 1.0000, @CreatedAt),
        (@SecondTaskVersionId, @MaterialId, @SecondTaskId, @MaterialVersionId, 1, 1,
         N'Napiši dvije rečenice o iskustvima koristeći Present Perfect.', N'OPEN_RESPONSE',
         3, 4, NULL,
         N'Obje rečenice koriste have/has + past participle i smislen kontekst iskustva.',
         4.0000, @CreatedAt);

    INSERT INTO [AssessableTaskVersionKnowledgeComponents]
        ([AssessableTaskVersionId], [KnowledgeComponentId])
    VALUES
        (@FirstTaskVersionId, @KnowledgeComponentId),
        (@SecondTaskVersionId, @KnowledgeComponentId);

    UPDATE [MaterialVersions]
    SET [Status] = 2,
        [ActivatedAtUtc] = DATEADD(minute, 4, @CreatedAt)
    WHERE [Id] = @MaterialVersionId;

    COMMIT TRANSACTION;
END;

SELECT @MaterialId AS [MaterialId], @OwnerId AS [OwnerTeacherId];
