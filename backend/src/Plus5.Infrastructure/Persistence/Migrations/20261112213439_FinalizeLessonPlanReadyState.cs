using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeLessonPlanReadyState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_LessonPlanVersions_ProtectSnapshots]
                ON [LessonPlanVersions]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE d.[Status] <> 1 AND i.[Id] IS NULL
                    ) THROW 51303, 'Ready and Superseded LessonPlanVersion snapshots cannot be deleted.', 1;

                    IF EXISTS (
                        SELECT 1 FROM deleted d JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE d.[Status] <> 1 AND (i.[LessonPlanId] <> d.[LessonPlanId]
                          OR i.[VersionNumber] <> d.[VersionNumber] OR i.[Goal] <> d.[Goal]
                          OR i.[TargetDurationMinutes] <> d.[TargetDurationMinutes]
                          OR i.[CreatedAtUtc] <> d.[CreatedAtUtc] OR i.[UpdatedAtUtc] <> d.[UpdatedAtUtc])
                    ) THROW 51303, 'Ready and Superseded LessonPlanVersion snapshots are immutable.', 1;

                    IF EXISTS (
                        SELECT 1 FROM deleted d JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE (d.[Status] = 1 AND i.[Status] NOT IN (1, 2))
                           OR (d.[Status] = 2 AND i.[Status] NOT IN (2, 3))
                           OR (d.[Status] = 3 AND i.[Status] <> 3)
                    ) THROW 51351, 'LessonPlanVersion lifecycle transition is invalid.', 1;

                    IF EXISTS (
                        SELECT 1 FROM deleted d JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE d.[Status] = 1 AND i.[Status] = 2
                          AND (NULLIF(LTRIM(RTRIM(i.[Goal])), '') IS NULL
                            OR NOT EXISTS (SELECT 1 FROM [LessonActivities] a WHERE a.[LessonPlanVersionId] = i.[Id])
                            OR EXISTS (
                                SELECT 1 FROM [LessonActivities] a WHERE a.[LessonPlanVersionId] = i.[Id]
                                GROUP BY a.[LessonPlanVersionId]
                                HAVING MIN(a.[SortOrder]) <> 0 OR MAX(a.[SortOrder]) <> COUNT_BIG(*) - 1
                                   OR SUM(a.[DurationMinutes]) <= 0)
                            OR EXISTS (
                                SELECT 1 FROM [LessonActivities] a
                                JOIN [LessonActivityMaterialVersions] am ON am.[LessonActivityId] = a.[Id]
                                WHERE a.[LessonPlanVersionId] = i.[Id]
                                  AND NOT EXISTS (SELECT 1 FROM [MaterialFiles] f
                                      WHERE f.[MaterialVersionId] = am.[MaterialVersionId] AND f.[Status] = 4)))
                    ) THROW 51350, 'LessonPlanVersion does not satisfy the Ready graph contract.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_LessonPlanVersions_ProtectSnapshots]
                ON [LessonPlanVersions]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE d.[Status] <> 1 AND (i.[Id] IS NULL OR i.[LessonPlanId] <> d.[LessonPlanId]
                          OR i.[VersionNumber] <> d.[VersionNumber] OR i.[Goal] <> d.[Goal]
                          OR i.[TargetDurationMinutes] <> d.[TargetDurationMinutes]
                          OR i.[CreatedAtUtc] <> d.[CreatedAtUtc] OR i.[UpdatedAtUtc] <> d.[UpdatedAtUtc])
                    ) THROW 51303, 'Ready and Superseded LessonPlanVersion snapshots are immutable.', 1;
                END
                """);
        }
    }
}
