using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumOutcomeHierarchy : Migration
    {
        private static readonly string[] CurriculumOutcomeIdentityColumns =
            ["CurriculumId", "Id"];
        private static readonly string[] CurriculumOutcomeOrderColumns =
            ["CurriculumId", "ParentOutcomeId", "SortOrder", "Id"];
        private static readonly string[] CurriculumOutcomeOfficialCodeColumns =
            ["CurriculumId", "OfficialCode"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurriculumOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersedesOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OfficialCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SourceAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumOutcomes", x => x.Id);
                    table.UniqueConstraint("AK_CurriculumOutcomes_Curriculum_Id", x => new { x.CurriculumId, x.Id });
                    table.CheckConstraint("CK_CurriculumOutcomes_NotOwnParent", "[ParentOutcomeId] IS NULL OR [ParentOutcomeId] <> [Id]");
                    table.CheckConstraint("CK_CurriculumOutcomes_NotOwnPredecessor", "[SupersedesOutcomeId] IS NULL OR [SupersedesOutcomeId] <> [Id]");
                    table.CheckConstraint("CK_CurriculumOutcomes_OfficialCodeProvenance", "[OfficialCode] IS NULL OR ([SourceAuthority] IS NOT NULL AND [SourceReference] IS NOT NULL)");
                    table.CheckConstraint("CK_CurriculumOutcomes_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomes_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomes_CurriculumOutcomes_CurriculumId_ParentOutcomeId",
                        columns: x => new { x.CurriculumId, x.ParentOutcomeId },
                        principalTable: "CurriculumOutcomes",
                        principalColumns: CurriculumOutcomeIdentityColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomes_CurriculumOutcomes_SupersedesOutcomeId",
                        column: x => x.SupersedesOutcomeId,
                        principalTable: "CurriculumOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumOutcomes_Curriculum_Parent_Sort_Id",
                table: "CurriculumOutcomes",
                columns: CurriculumOutcomeOrderColumns);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumOutcomes_SupersedesOutcomeId",
                table: "CurriculumOutcomes",
                column: "SupersedesOutcomeId");

            migrationBuilder.CreateIndex(
                name: "UX_CurriculumOutcomes_Curriculum_OfficialCode",
                table: "CurriculumOutcomes",
                columns: CurriculumOutcomeOfficialCodeColumns,
                unique: true,
                filter: "[OfficialCode] IS NOT NULL");

            migrationBuilder.Sql(
                """
                EXEC(N'
                CREATE TRIGGER [TR_CurriculumOutcomes_ValidateHierarchy]
                ON [CurriculumOutcomes]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [inserted] AS [i]
                        INNER JOIN [CurriculumOutcomes] AS [s]
                            ON [s].[Id] = [i].[SupersedesOutcomeId]
                        INNER JOIN [Curricula] AS [currentCurriculum]
                            ON [currentCurriculum].[Id] = [i].[CurriculumId]
                        INNER JOIN [Curricula] AS [previousCurriculum]
                            ON [previousCurriculum].[Id] = [s].[CurriculumId]
                        WHERE [currentCurriculum].[Code] <> [previousCurriculum].[Code]
                            OR [currentCurriculum].[Version] = [previousCurriculum].[Version]
                    )
                    BEGIN
                        THROW 51000, ''Curriculum outcome supersession must stay in the same curriculum family and cross versions.'', 1;
                    END;

                    DECLARE @HasCycle bit = 0;

                    ;WITH [Ancestors] AS
                    (
                        SELECT [i].[Id] AS [StartId], [i].[ParentOutcomeId] AS [AncestorId]
                        FROM [inserted] AS [i]
                        WHERE [i].[ParentOutcomeId] IS NOT NULL

                        UNION ALL

                        SELECT [a].[StartId], [p].[ParentOutcomeId]
                        FROM [Ancestors] AS [a]
                        INNER JOIN [CurriculumOutcomes] AS [p]
                            ON [p].[Id] = [a].[AncestorId]
                        WHERE [a].[AncestorId] <> [a].[StartId]
                            AND [p].[ParentOutcomeId] IS NOT NULL
                    )
                    SELECT TOP (1) @HasCycle = 1
                    FROM [Ancestors]
                    WHERE [AncestorId] = [StartId]
                    OPTION (MAXRECURSION 32767);

                    IF @HasCycle = 1
                    BEGIN
                        THROW 51001, ''Curriculum outcome hierarchy cannot contain a cycle.'', 1;
                    END;
                END;
                ');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_CurriculumOutcomes_ValidateHierarchy];");

            migrationBuilder.DropTable(
                name: "CurriculumOutcomes");
        }
    }
}
