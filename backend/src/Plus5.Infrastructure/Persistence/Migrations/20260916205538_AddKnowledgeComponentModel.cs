using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeComponentModel : Migration
    {
        private static readonly string[] KnowledgeAreaIdentityColumns =
            ["KnowledgeModelId", "Id"];
        private static readonly string[] KnowledgeAreaOrderColumns =
            ["KnowledgeModelId", "SortOrder", "Id"];
        private static readonly string[] KnowledgeComponentIdentityColumns =
            ["KnowledgeModelId", "KnowledgeAreaId", "Id"];
        private static readonly string[] KnowledgeComponentOrderColumns =
            ["KnowledgeModelId", "KnowledgeAreaId", "ParentComponentId", "SortOrder", "Id"];
        private static readonly string[] KnowledgeModelIdentityColumns =
            ["Code", "Version"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeModels", x => x.Id);
                    table.CheckConstraint("CK_KnowledgeModels_Status", "[Status] IN (1, 2, 3)");
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeAreas", x => x.Id);
                    table.UniqueConstraint("AK_KnowledgeAreas_KnowledgeModel_Id", x => new { x.KnowledgeModelId, x.Id });
                    table.CheckConstraint("CK_KnowledgeAreas_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_KnowledgeAreas_KnowledgeModels_KnowledgeModelId",
                        column: x => x.KnowledgeModelId,
                        principalTable: "KnowledgeModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersedesKnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeComponents", x => x.Id);
                    table.UniqueConstraint("AK_KnowledgeComponents_Model_Area_Id", x => new { x.KnowledgeModelId, x.KnowledgeAreaId, x.Id });
                    table.CheckConstraint("CK_KnowledgeComponents_NotOwnParent", "[ParentComponentId] IS NULL OR [ParentComponentId] <> [Id]");
                    table.CheckConstraint("CK_KnowledgeComponents_NotOwnPredecessor", "[SupersedesKnowledgeComponentId] IS NULL OR [SupersedesKnowledgeComponentId] <> [Id]");
                    table.CheckConstraint("CK_KnowledgeComponents_SortOrder", "[SortOrder] >= 0");
                    table.CheckConstraint("CK_KnowledgeComponents_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_KnowledgeComponents_KnowledgeAreas_KnowledgeModelId_KnowledgeAreaId",
                        columns: x => new { x.KnowledgeModelId, x.KnowledgeAreaId },
                        principalTable: "KnowledgeAreas",
                        principalColumns: KnowledgeAreaIdentityColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeComponents_KnowledgeComponents_KnowledgeModelId_KnowledgeAreaId_ParentComponentId",
                        columns: x => new { x.KnowledgeModelId, x.KnowledgeAreaId, x.ParentComponentId },
                        principalTable: "KnowledgeComponents",
                        principalColumns: KnowledgeComponentIdentityColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeComponents_KnowledgeComponents_SupersedesKnowledgeComponentId",
                        column: x => x.SupersedesKnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeComponents_KnowledgeModels_KnowledgeModelId",
                        column: x => x.KnowledgeModelId,
                        principalTable: "KnowledgeModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumOutcomeKnowledgeComponents",
                columns: table => new
                {
                    CurriculumOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumOutcomeKnowledgeComponents", x => new { x.CurriculumOutcomeId, x.KnowledgeComponentId });
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomeKnowledgeComponents_CurriculumOutcomes_CurriculumOutcomeId",
                        column: x => x.CurriculumOutcomeId,
                        principalTable: "CurriculumOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomeKnowledgeComponents_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumOutcomeKnowledgeComponents_ComponentId",
                table: "CurriculumOutcomeKnowledgeComponents",
                column: "KnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeAreas_Model_Sort_Id",
                table: "KnowledgeAreas",
                columns: KnowledgeAreaOrderColumns);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeComponents_Model_Area_Parent_Sort_Id",
                table: "KnowledgeComponents",
                columns: KnowledgeComponentOrderColumns);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeComponents_SupersedesComponentId",
                table: "KnowledgeComponents",
                column: "SupersedesKnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "UX_KnowledgeModels_Code_Version",
                table: "KnowledgeModels",
                columns: KnowledgeModelIdentityColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                EXEC(N'
                CREATE TRIGGER [TR_KnowledgeModels_ProtectLifecycle]
                ON [KnowledgeModels]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] AS [d]
                        LEFT JOIN [inserted] AS [i] ON [i].[Id] = [d].[Id]
                        WHERE [i].[Id] IS NULL AND [d].[Status] <> 1
                    )
                    BEGIN
                        THROW 51100, ''Published or retired knowledge models cannot be hard-deleted.'', 1;
                    END;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] AS [d]
                        INNER JOIN [inserted] AS [i] ON [i].[Id] = [d].[Id]
                        WHERE NOT
                        (
                            ([d].[Status] = 1 AND [i].[Status] IN (1, 2))
                            OR ([d].[Status] = 2 AND [i].[Status] IN (2, 3))
                            OR ([d].[Status] = 3 AND [i].[Status] = 3)
                        )
                    )
                    BEGIN
                        THROW 51101, ''Knowledge model status transition is not allowed.'', 1;
                    END;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] AS [d]
                        INNER JOIN [inserted] AS [i] ON [i].[Id] = [d].[Id]
                        WHERE [d].[Status] IN (2, 3)
                            AND ([d].[Code] <> [i].[Code] OR [d].[Version] <> [i].[Version])
                    )
                    BEGIN
                        THROW 51102, ''Published or retired knowledge model identity is immutable.'', 1;
                    END;
                END;
                ');

                EXEC(N'
                CREATE TRIGGER [TR_KnowledgeAreas_ProtectPublishedStructure]
                ON [KnowledgeAreas]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [inserted] AS [i]
                        INNER JOIN [KnowledgeModels] AS [m] ON [m].[Id] = [i].[KnowledgeModelId]
                        WHERE [m].[Status] <> 1
                    )
                    OR EXISTS
                    (
                        SELECT 1
                        FROM [deleted] AS [d]
                        INNER JOIN [KnowledgeModels] AS [m] ON [m].[Id] = [d].[KnowledgeModelId]
                        WHERE [m].[Status] <> 1
                    )
                    BEGIN
                        THROW 51103, ''Published or retired knowledge area structure is immutable.'', 1;
                    END;
                END;
                ');

                EXEC(N'
                CREATE TRIGGER [TR_KnowledgeComponents_ValidateIntegrity]
                ON [KnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [inserted] AS [i]
                        INNER JOIN [KnowledgeModels] AS [m] ON [m].[Id] = [i].[KnowledgeModelId]
                        WHERE [m].[Status] <> 1
                    )
                    OR EXISTS
                    (
                        SELECT 1
                        FROM [deleted] AS [d]
                        INNER JOIN [KnowledgeModels] AS [m] ON [m].[Id] = [d].[KnowledgeModelId]
                        WHERE [m].[Status] <> 1
                    )
                    BEGIN
                        THROW 51104, ''Published or retired knowledge component structure is immutable.'', 1;
                    END;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [inserted] AS [i]
                        INNER JOIN [KnowledgeComponents] AS [s]
                            ON [s].[Id] = [i].[SupersedesKnowledgeComponentId]
                        INNER JOIN [KnowledgeModels] AS [currentModel]
                            ON [currentModel].[Id] = [i].[KnowledgeModelId]
                        INNER JOIN [KnowledgeModels] AS [previousModel]
                            ON [previousModel].[Id] = [s].[KnowledgeModelId]
                        WHERE [currentModel].[Code] <> [previousModel].[Code]
                            OR [currentModel].[Version] = [previousModel].[Version]
                    )
                    BEGIN
                        THROW 51105, ''Knowledge component lineage must stay in the same model family and cross versions.'', 1;
                    END;

                    DECLARE @HasCycle bit = 0;

                    ;WITH [Ancestors] AS
                    (
                        SELECT [i].[Id] AS [StartId], [i].[ParentComponentId] AS [AncestorId]
                        FROM [inserted] AS [i]
                        WHERE [i].[ParentComponentId] IS NOT NULL

                        UNION ALL

                        SELECT [a].[StartId], [p].[ParentComponentId]
                        FROM [Ancestors] AS [a]
                        INNER JOIN [KnowledgeComponents] AS [p]
                            ON [p].[Id] = [a].[AncestorId]
                        WHERE [a].[AncestorId] <> [a].[StartId]
                            AND [p].[ParentComponentId] IS NOT NULL
                    )
                    SELECT TOP (1) @HasCycle = 1
                    FROM [Ancestors]
                    WHERE [AncestorId] = [StartId]
                    OPTION (MAXRECURSION 32767);

                    IF @HasCycle = 1
                    BEGIN
                        THROW 51106, ''Knowledge component hierarchy cannot contain a cycle.'', 1;
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
                DROP TRIGGER IF EXISTS [TR_KnowledgeComponents_ValidateIntegrity];
                DROP TRIGGER IF EXISTS [TR_KnowledgeAreas_ProtectPublishedStructure];
                DROP TRIGGER IF EXISTS [TR_KnowledgeModels_ProtectLifecycle];
                """);

            migrationBuilder.DropTable(
                name: "CurriculumOutcomeKnowledgeComponents");

            migrationBuilder.DropTable(
                name: "KnowledgeComponents");

            migrationBuilder.DropTable(
                name: "KnowledgeAreas");

            migrationBuilder.DropTable(
                name: "KnowledgeModels");
        }
    }
}
