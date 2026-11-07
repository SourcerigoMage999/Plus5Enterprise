using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF migration scaffolding uses inline metadata arrays.

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonActivityDetailEditingAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LessonActivities",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LessonActivities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurposeDescription",
                table: "LessonActivities",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowProgressOnBoard",
                table: "LessonActivities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowStudentInstructionsOnBoard",
                table: "LessonActivities",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTeacherInstructionsOnBoard",
                table: "LessonActivities",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentInstructions",
                table: "LessonActivities",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherInstructions",
                table: "LessonActivities",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkMode",
                table: "LessonActivities",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "LessonActivityAiSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BasedOnRowVersion = table.Column<byte[]>(type: "binary(8)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PromptTemplateVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    ReplacesSuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonActivityAiSuggestions", x => x.Id);
                    table.CheckConstraint("CK_LessonActivityAiSuggestions_ContentLifecycle", "([Status] = 1 AND [ContentJson] IS NOT NULL AND ISJSON([ContentJson]) = 1 AND [ResolvedAtUtc] IS NULL) OR ([Status] BETWEEN 2 AND 6 AND [ContentJson] IS NULL AND [ResolvedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_LessonActivityAiSuggestions_Expiry", "[ExpiresAtUtc] > [GeneratedAtUtc]");
                    table.CheckConstraint("CK_LessonActivityAiSuggestions_Status", "[Status] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_LessonActivityAiSuggestions_LessonActivityAiSuggestions_ReplacesSuggestionId",
                        column: x => x.ReplacesSuggestionId,
                        principalTable: "LessonActivityAiSuggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonActivityAiSuggestions_LessonPlanVersions_LessonPlanId_LessonPlanVersionId",
                        columns: x => new { x.LessonPlanId, x.LessonPlanVersionId },
                        principalTable: "LessonPlanVersions",
                        principalColumns: new[] { "LessonPlanId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonActivityAiSuggestions_LessonPlans_TeacherAccountId_LessonPlanId",
                        columns: x => new { x.TeacherAccountId, x.LessonPlanId },
                        principalTable: "LessonPlans",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonActivityAiSuggestions_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonActivities_WorkMode",
                table: "LessonActivities",
                sql: "[WorkMode] BETWEEN 1 AND 3");

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityAiSuggestions_LessonActivityId_Status",
                table: "LessonActivityAiSuggestions",
                columns: new[] { "LessonActivityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityAiSuggestions_LessonPlanId_LessonPlanVersionId",
                table: "LessonActivityAiSuggestions",
                columns: new[] { "LessonPlanId", "LessonPlanVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityAiSuggestions_ReplacesSuggestionId",
                table: "LessonActivityAiSuggestions",
                column: "ReplacesSuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityAiSuggestions_TeacherAccountId_LessonPlanId",
                table: "LessonActivityAiSuggestions",
                columns: new[] { "TeacherAccountId", "LessonPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityAiSuggestions_TeacherAccountId_Status_GeneratedAtUtc",
                table: "LessonActivityAiSuggestions",
                columns: new[] { "TeacherAccountId", "Status", "GeneratedAtUtc" });

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonActivityAiSuggestions_Validate]
                ON [dbo].[LessonActivityAiSuggestions]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        LEFT JOIN [dbo].[LessonPlans] p
                          ON p.[Id] = i.[LessonPlanId]
                         AND p.[TeacherAccountId] = i.[TeacherAccountId]
                         AND p.[ArchivedAtUtc] IS NULL
                        LEFT JOIN [dbo].[LessonPlanVersions] v
                          ON v.[Id] = i.[LessonPlanVersionId]
                         AND v.[LessonPlanId] = i.[LessonPlanId]
                         AND v.[Status] = 1
                        LEFT JOIN [dbo].[LessonActivities] a
                          ON a.[Id] = i.[LessonActivityId]
                         AND a.[LessonPlanVersionId] = i.[LessonPlanVersionId]
                        WHERE p.[Id] IS NULL OR v.[Id] IS NULL OR a.[Id] IS NULL
                    )
                        THROW 51340, 'Lesson Activity AI suggestion must target an owned Draft activity.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_LessonActivityAiSuggestions_Validate]");
            migrationBuilder.DropTable(
                name: "LessonActivityAiSuggestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LessonActivities_WorkMode",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "PurposeDescription",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "ShowProgressOnBoard",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "ShowStudentInstructionsOnBoard",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "ShowTeacherInstructionsOnBoard",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "StudentInstructions",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "TeacherInstructions",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "WorkMode",
                table: "LessonActivities");
        }
    }
}
#pragma warning restore CA1861
