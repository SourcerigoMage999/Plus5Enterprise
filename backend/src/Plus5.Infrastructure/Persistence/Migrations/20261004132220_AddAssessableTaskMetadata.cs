using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessableTaskMetadata : Migration
    {
        private static readonly string[] MaterialTaskColumns =
            ["MaterialId", "AssessableTaskId"];
        private static readonly string[] MaterialVersionColumns =
            ["MaterialId", "MaterialVersionId"];
        private static readonly string[] MaterialVersionOrderColumns =
            ["MaterialVersionId", "SortOrder", "Id"];
        private static readonly string[] TaskMaterialVersionColumns =
            ["AssessableTaskId", "MaterialVersionId"];
        private static readonly string[] TaskVersionNumberColumns =
            ["AssessableTaskId", "VersionNumber"];
        private static readonly string[] MaterialPrincipalColumns = ["MaterialId", "Id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessableTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessableTasks", x => x.Id);
                    table.UniqueConstraint("AK_AssessableTasks_Material_Id", x => new { x.MaterialId, x.Id });
                    table.ForeignKey(
                        name: "FK_AssessableTasks_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessableTaskVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessableTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    TaskTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EvaluationCriterion = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MaxPoints = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessableTaskVersions", x => x.Id);
                    table.UniqueConstraint("AK_AssessableTaskVersions_MaterialVersion_Id", x => new { x.MaterialVersionId, x.Id });
                    table.CheckConstraint("CK_AssessableTaskVersions_Difficulty", "[Difficulty] >= 1 AND [Difficulty] <= 5");
                    table.CheckConstraint("CK_AssessableTaskVersions_Evaluation", "[CorrectAnswer] IS NOT NULL OR [EvaluationCriterion] IS NOT NULL");
                    table.CheckConstraint("CK_AssessableTaskVersions_EvidenceType", "[EvidenceType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_AssessableTaskVersions_MaxPoints", "[MaxPoints] > 0");
                    table.CheckConstraint("CK_AssessableTaskVersions_SortOrder", "[SortOrder] >= 0");
                    table.CheckConstraint("CK_AssessableTaskVersions_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_AssessableTaskVersions_AssessableTasks_MaterialId_AssessableTaskId",
                        columns: x => new { x.MaterialId, x.AssessableTaskId },
                        principalTable: "AssessableTasks",
                        principalColumns: MaterialPrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessableTaskVersions_MaterialVersions_MaterialId_MaterialVersionId",
                        columns: x => new { x.MaterialId, x.MaterialVersionId },
                        principalTable: "MaterialVersions",
                        principalColumns: MaterialPrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessableTaskVersionKnowledgeComponents",
                columns: table => new
                {
                    AssessableTaskVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessableTaskVersionKnowledgeComponents", x => new { x.AssessableTaskVersionId, x.KnowledgeComponentId });
                    table.ForeignKey(
                        name: "FK_AssessableTaskVersionKnowledgeComponents_AssessableTaskVersions_AssessableTaskVersionId",
                        column: x => x.AssessableTaskVersionId,
                        principalTable: "AssessableTaskVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessableTaskVersionKnowledgeComponents_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessableTasks_MaterialId",
                table: "AssessableTasks",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessableTaskVersionKnowledgeComponents_ComponentId",
                table: "AssessableTaskVersionKnowledgeComponents",
                column: "KnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessableTaskVersions_MaterialId_AssessableTaskId",
                table: "AssessableTaskVersions",
                columns: MaterialTaskColumns);

            migrationBuilder.CreateIndex(
                name: "IX_AssessableTaskVersions_MaterialId_MaterialVersionId",
                table: "AssessableTaskVersions",
                columns: MaterialVersionColumns);

            migrationBuilder.CreateIndex(
                name: "IX_AssessableTaskVersions_MaterialVersion_Order",
                table: "AssessableTaskVersions",
                columns: MaterialVersionOrderColumns);

            migrationBuilder.CreateIndex(
                name: "UX_AssessableTaskVersions_Task_MaterialVersion",
                table: "AssessableTaskVersions",
                columns: TaskMaterialVersionColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AssessableTaskVersions_Task_VersionNumber",
                table: "AssessableTaskVersions",
                columns: TaskVersionNumberColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_AssessableTaskVersions_ProtectSnapshot]
                ON [AssessableTaskVersions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS task_version
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS task_version
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                        THROW 51231, 'Assessable task metadata is mutable only within a draft material version.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted
                        WHERE [TaskTypeCode] = ''
                           OR [TaskTypeCode] COLLATE Latin1_General_100_BIN2
                              LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2
                           OR DATALENGTH([Prompt]) = 0
                           OR ([CorrectAnswer] IS NOT NULL AND DATALENGTH([CorrectAnswer]) = 0)
                           OR ([EvaluationCriterion] IS NOT NULL AND DATALENGTH([EvaluationCriterion]) = 0))
                        THROW 51231, 'Assessable task text and type code must use canonical values.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_AssessableTaskVersionKnowledgeComponents_ProtectSnapshot]
                ON [AssessableTaskVersionKnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS mapping
                        INNER JOIN [AssessableTaskVersions] AS task_version
                            ON task_version.[Id] = mapping.[AssessableTaskVersionId]
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS mapping
                        INNER JOIN [AssessableTaskVersions] AS task_version
                            ON task_version.[Id] = mapping.[AssessableTaskVersionId]
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                        THROW 51231, 'Assessable task knowledge mappings are mutable only within a draft material version.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS mapping
                        INNER JOIN [KnowledgeComponents] AS component
                            ON component.[Id] = mapping.[KnowledgeComponentId]
                        INNER JOIN [KnowledgeModels] AS model
                            ON model.[Id] = component.[KnowledgeModelId]
                        WHERE model.[Status] = 1
                           OR EXISTS (
                               SELECT 1
                               FROM [KnowledgeComponents] AS child
                               WHERE child.[ParentComponentId] = component.[Id]))
                        THROW 51232, 'Assessable task evidence targets must be leaf components from published or retired models.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialVersions_ValidateAssessableTasks]
                ON [MaterialVersions]
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row
                            ON previous_row.[Id] = current_row.[Id]
                        INNER JOIN [AssessableTaskVersions] AS task_version
                            ON task_version.[MaterialVersionId] = current_row.[Id]
                        WHERE previous_row.[Status] = 1
                          AND current_row.[Status] = 2
                          AND NOT EXISTS (
                              SELECT 1
                              FROM [AssessableTaskVersionKnowledgeComponents] AS mapping
                              WHERE mapping.[AssessableTaskVersionId] = task_version.[Id]))
                        THROW 51233, 'Every assessable task requires at least one knowledge component before material activation.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_MaterialVersions_ValidateAssessableTasks];");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_AssessableTaskVersionKnowledgeComponents_ProtectSnapshot];");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_AssessableTaskVersions_ProtectSnapshot];");

            migrationBuilder.DropTable(
                name: "AssessableTaskVersionKnowledgeComponents");

            migrationBuilder.DropTable(
                name: "AssessableTaskVersions");

            migrationBuilder.DropTable(
                name: "AssessableTasks");
        }
    }
}
