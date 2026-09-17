using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeMappingLifecycleGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                EXEC(N'
                CREATE TRIGGER [TR_CurriculumOutcomeKnowledgeComponents_ProtectPublishedModel]
                ON [CurriculumOutcomeKnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [inserted] AS [i]
                        INNER JOIN [KnowledgeComponents] AS [c]
                            ON [c].[Id] = [i].[KnowledgeComponentId]
                        INNER JOIN [KnowledgeModels] AS [m]
                            ON [m].[Id] = [c].[KnowledgeModelId]
                        WHERE [m].[Status] <> 1
                    )
                    OR EXISTS
                    (
                        SELECT 1
                        FROM [deleted] AS [d]
                        INNER JOIN [KnowledgeComponents] AS [c]
                            ON [c].[Id] = [d].[KnowledgeComponentId]
                        INNER JOIN [KnowledgeModels] AS [m]
                            ON [m].[Id] = [c].[KnowledgeModelId]
                        WHERE [m].[Status] <> 1
                    )
                    BEGIN
                        THROW 51107, ''Published or retired knowledge model curriculum mappings are immutable.'', 1;
                    END;
                END;
                ');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS [TR_CurriculumOutcomeKnowledgeComponents_ProtectPublishedModel];
                """);
        }
    }
}
