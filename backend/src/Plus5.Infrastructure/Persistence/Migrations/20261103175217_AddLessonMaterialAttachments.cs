using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonMaterialAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonActivityMaterialVersions",
                columns: table => new
                {
                    LessonActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonActivityMaterialVersions", x => new { x.LessonActivityId, x.MaterialVersionId });
                    table.ForeignKey(
                        name: "FK_LessonActivityMaterialVersions_LessonActivities_LessonActivityId",
                        column: x => x.LessonActivityId,
                        principalTable: "LessonActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonActivityMaterialVersions_MaterialVersions_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityMaterialVersions_MaterialVersionId",
                table: "LessonActivityMaterialVersions",
                column: "MaterialVersionId");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_LessonActivityMaterialVersions_Validate]
                ON [LessonActivityMaterialVersions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT [LessonActivityId] FROM inserted
                            UNION
                            SELECT [LessonActivityId] FROM deleted
                        ) AS changed
                        INNER JOIN [LessonActivities] AS activity
                            ON activity.[Id] = changed.[LessonActivityId]
                        INNER JOIN [LessonPlanVersions] AS plan_version
                            ON plan_version.[Id] = activity.[LessonPlanVersionId]
                        WHERE plan_version.[Status] <> 1)
                    BEGIN
                        THROW 51320, 'Lesson activity material attachments are mutable only on Draft lesson plan versions.', 1;
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS attachment
                        INNER JOIN [LessonActivities] AS activity
                            ON activity.[Id] = attachment.[LessonActivityId]
                        INNER JOIN [LessonPlanVersions] AS plan_version
                            ON plan_version.[Id] = activity.[LessonPlanVersionId]
                        INNER JOIN [LessonPlans] AS lesson_plan
                            ON lesson_plan.[Id] = plan_version.[LessonPlanId]
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = attachment.[MaterialVersionId]
                        INNER JOIN [Materials] AS material
                            ON material.[Id] = material_version.[MaterialId]
                        WHERE material.[Status] <> 1
                           OR material.[ArchivedAtUtc] IS NOT NULL
                           OR material.[CurrentVersionId] <> material_version.[Id]
                           OR material_version.[Status] <> 2
                           OR NOT EXISTS (
                                SELECT 1
                                FROM [MaterialFiles] AS material_file
                                WHERE material_file.[MaterialVersionId] = material_version.[Id]
                                  AND material_file.[Status] = 4)
                           OR (
                                material.[OwnerTeacherId] <> lesson_plan.[TeacherAccountId]
                                AND NOT (
                                    material.[Visibility] = 2
                                    AND EXISTS (
                                        SELECT 1
                                        FROM [MaterialShares] AS material_share
                                        WHERE material_share.[MaterialId] = material.[Id]
                                          AND material_share.[SharedWithTeacherId] = lesson_plan.[TeacherAccountId]
                                          AND material_share.[Permission] = 2)))
                    )
                    BEGIN
                        THROW 51321, 'Material version is not available to this lesson plan teacher.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_LessonActivityMaterialVersions_Validate];");

            migrationBuilder.DropTable(
                name: "LessonActivityMaterialVersions");
        }
    }
}
