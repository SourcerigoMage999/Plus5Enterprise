using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialMetadataMapping : Migration
    {
        private static readonly string[] TeacherProgramColumns =
            ["CreatedByTeacherId", "ProgramId"];
        private static readonly string[] ProgramPrincipalColumns =
            ["TeacherAccountId", "Id"];
        private static readonly string[] TagLookupColumns =
            ["NormalizedName", "MaterialVersionId"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LearningGoal",
                table: "MaterialVersions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProficiencyLevelId",
                table: "MaterialVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramId",
                table: "MaterialVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolGradeId",
                table: "MaterialVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialVersionCurriculumOutcomes",
                columns: table => new
                {
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVersionCurriculumOutcomes", x => new { x.MaterialVersionId, x.CurriculumOutcomeId });
                    table.ForeignKey(
                        name: "FK_MaterialVersionCurriculumOutcomes_CurriculumOutcomes_CurriculumOutcomeId",
                        column: x => x.CurriculumOutcomeId,
                        principalTable: "CurriculumOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialVersionCurriculumOutcomes_MaterialVersions_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVersionKnowledgeComponents",
                columns: table => new
                {
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVersionKnowledgeComponents", x => new { x.MaterialVersionId, x.KnowledgeComponentId });
                    table.ForeignKey(
                        name: "FK_MaterialVersionKnowledgeComponents_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialVersionKnowledgeComponents_MaterialVersions_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVersionTags",
                columns: table => new
                {
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVersionTags", x => new { x.MaterialVersionId, x.NormalizedName });
                    table.ForeignKey(
                        name: "FK_MaterialVersionTags_MaterialVersions_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersions_ProficiencyLevelId",
                table: "MaterialVersions",
                column: "ProficiencyLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersions_SchoolGradeId",
                table: "MaterialVersions",
                column: "SchoolGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersions_Teacher_ProgramId",
                table: "MaterialVersions",
                columns: TeacherProgramColumns);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersionCurriculumOutcomes_OutcomeId",
                table: "MaterialVersionCurriculumOutcomes",
                column: "CurriculumOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersionKnowledgeComponents_ComponentId",
                table: "MaterialVersionKnowledgeComponents",
                column: "KnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersionTags_NormalizedName_VersionId",
                table: "MaterialVersionTags",
                columns: TagLookupColumns);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialVersions_ProficiencyLevels_ProficiencyLevelId",
                table: "MaterialVersions",
                column: "ProficiencyLevelId",
                principalTable: "ProficiencyLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialVersions_Programs_CreatedByTeacherId_ProgramId",
                table: "MaterialVersions",
                columns: TeacherProgramColumns,
                principalTable: "Programs",
                principalColumns: ProgramPrincipalColumns,
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialVersions_SchoolGrades_SchoolGradeId",
                table: "MaterialVersions",
                column: "SchoolGradeId",
                principalTable: "SchoolGrades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialVersionCurriculumOutcomes_ProtectSnapshot]
                ON [MaterialVersionCurriculumOutcomes]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS mapping
                        INNER JOIN [MaterialVersions] AS version
                            ON version.[Id] = mapping.[MaterialVersionId]
                        WHERE version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS mapping
                        INNER JOIN [MaterialVersions] AS version
                            ON version.[Id] = mapping.[MaterialVersionId]
                        WHERE version.[Status] <> 1)
                        THROW 51224, 'Material metadata mappings are mutable only for draft versions.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialVersionKnowledgeComponents_ProtectSnapshot]
                ON [MaterialVersionKnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS mapping
                        INNER JOIN [MaterialVersions] AS version
                            ON version.[Id] = mapping.[MaterialVersionId]
                        WHERE version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS mapping
                        INNER JOIN [MaterialVersions] AS version
                            ON version.[Id] = mapping.[MaterialVersionId]
                        WHERE version.[Status] <> 1)
                        THROW 51224, 'Material metadata mappings are mutable only for draft versions.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS mapping
                        INNER JOIN [KnowledgeComponents] AS component
                            ON component.[Id] = mapping.[KnowledgeComponentId]
                        INNER JOIN [KnowledgeModels] AS model
                            ON model.[Id] = component.[KnowledgeModelId]
                        WHERE model.[Status] = 1)
                        THROW 51225, 'Material metadata cannot reference a draft knowledge model.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialVersionTags_ProtectSnapshot]
                ON [MaterialVersionTags]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS tag
                        INNER JOIN [MaterialVersions] AS version
                            ON version.[Id] = tag.[MaterialVersionId]
                        WHERE version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS tag
                        INNER JOIN [MaterialVersions] AS version
                            ON version.[Id] = tag.[MaterialVersionId]
                        WHERE version.[Status] <> 1)
                        THROW 51224, 'Material metadata mappings are mutable only for draft versions.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted
                        WHERE DATALENGTH([Name]) <> DATALENGTH(LTRIM(RTRIM([Name])))
                           OR [NormalizedName] COLLATE Latin1_General_100_BIN2
                              <> UPPER([Name]) COLLATE Latin1_General_100_BIN2)
                        THROW 51226, 'Material tag display and normalized names must be canonical.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_MaterialVersionTags_ProtectSnapshot];");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_MaterialVersionKnowledgeComponents_ProtectSnapshot];");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_MaterialVersionCurriculumOutcomes_ProtectSnapshot];");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialVersions_ProficiencyLevels_ProficiencyLevelId",
                table: "MaterialVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialVersions_Programs_CreatedByTeacherId_ProgramId",
                table: "MaterialVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialVersions_SchoolGrades_SchoolGradeId",
                table: "MaterialVersions");

            migrationBuilder.DropTable(
                name: "MaterialVersionCurriculumOutcomes");

            migrationBuilder.DropTable(
                name: "MaterialVersionKnowledgeComponents");

            migrationBuilder.DropTable(
                name: "MaterialVersionTags");

            migrationBuilder.DropIndex(
                name: "IX_MaterialVersions_ProficiencyLevelId",
                table: "MaterialVersions");

            migrationBuilder.DropIndex(
                name: "IX_MaterialVersions_SchoolGradeId",
                table: "MaterialVersions");

            migrationBuilder.DropIndex(
                name: "IX_MaterialVersions_Teacher_ProgramId",
                table: "MaterialVersions");

            migrationBuilder.DropColumn(
                name: "LearningGoal",
                table: "MaterialVersions");

            migrationBuilder.DropColumn(
                name: "ProficiencyLevelId",
                table: "MaterialVersions");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "MaterialVersions");

            migrationBuilder.DropColumn(
                name: "SchoolGradeId",
                table: "MaterialVersions");
        }
    }
}
