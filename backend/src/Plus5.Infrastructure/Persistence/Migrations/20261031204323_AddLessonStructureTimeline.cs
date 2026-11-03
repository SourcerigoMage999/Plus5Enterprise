using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration array arguments

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonStructureTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetDurationMinutes",
                table: "LessonPlanVersions",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "LessonPlanVersions",
                type: "datetimeoffset(7)",
                precision: 7,
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "KnowledgeBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentPublishedVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBlocks", x => x.Id);
                    table.CheckConstraint("CK_KnowledgeBlocks_Status", "[Status] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBlockVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MinDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    OptimalDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    MaxDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ParticipantApplicability = table.Column<int>(type: "int", nullable: false),
                    HomeworkMode = table.Column<int>(type: "int", nullable: false),
                    AssessmentCapability = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBlockVersions", x => x.Id);
                    table.UniqueConstraint("AK_KnowledgeBlockVersions_KnowledgeBlockId_Id", x => new { x.KnowledgeBlockId, x.Id });
                    table.CheckConstraint("CK_KnowledgeBlockVersions_Duration", "([Status] = 1 AND [MinDurationMinutes] IS NULL AND [OptimalDurationMinutes] IS NULL AND [MaxDurationMinutes] IS NULL) OR ([Status] IN (2, 3) AND [MinDurationMinutes] BETWEEN 1 AND 480 AND [OptimalDurationMinutes] BETWEEN [MinDurationMinutes] AND 480 AND [MaxDurationMinutes] BETWEEN [OptimalDurationMinutes] AND 480)");
                    table.CheckConstraint("CK_KnowledgeBlockVersions_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_KnowledgeBlockVersions_KnowledgeBlocks_KnowledgeBlockId",
                        column: x => x.KnowledgeBlockId,
                        principalTable: "KnowledgeBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeBlockVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonActivities", x => x.Id);
                    table.UniqueConstraint("AK_LessonActivities_LessonPlanVersionId_Id", x => new { x.LessonPlanVersionId, x.Id });
                    table.CheckConstraint("CK_LessonActivities_Duration", "[DurationMinutes] BETWEEN 1 AND 480");
                    table.CheckConstraint("CK_LessonActivities_Purpose", "[Purpose] BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_LessonActivities_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_LessonActivities_KnowledgeBlockVersions_KnowledgeBlockVersionId",
                        column: x => x.KnowledgeBlockVersionId,
                        principalTable: "KnowledgeBlockVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonActivities_LessonPlanVersions_LessonPlanVersionId",
                        column: x => x.LessonPlanVersionId,
                        principalTable: "LessonPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonActivityKnowledgeComponents",
                columns: table => new
                {
                    LessonActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonActivityKnowledgeComponents", x => new { x.LessonActivityId, x.KnowledgeComponentId });
                    table.ForeignKey(
                        name: "FK_LessonActivityKnowledgeComponents_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonActivityKnowledgeComponents_LessonActivities_LessonActivityId",
                        column: x => x.LessonActivityId,
                        principalTable: "LessonActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "KnowledgeBlocks",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "CurrentPublishedVersionId", "Status" },
                values: new object[,]
                {
                    { new Guid("95000000-0000-0000-0000-000000000001"), "KB-001", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000002"), "KB-002", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000003"), "KB-003", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000004"), "KB-004", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000005"), "KB-005", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000006"), "KB-006", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000007"), "KB-007", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000008"), "KB-008", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000009"), "KB-009", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000010"), "KB-010", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000011"), "KB-011", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000012"), "KB-012", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000013"), "KB-013", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000014"), "KB-014", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000015"), "KB-015", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000016"), "KB-016", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000017"), "KB-017", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000018"), "KB-018", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000019"), "KB-019", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000020"), "KB-020", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000021"), "KB-021", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000022"), "KB-022", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000023"), "KB-023", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000024"), "KB-024", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("95000000-0000-0000-0000-000000000025"), "KB-025", new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.InsertData(
                table: "KnowledgeBlockVersions",
                columns: new[] { "Id", "AssessmentCapability", "CreatedAtUtc", "DisplayName", "HomeworkMode", "KnowledgeBlockId", "MaxDurationMinutes", "MinDurationMinutes", "OptimalDurationMinutes", "ParticipantApplicability", "PublishedAtUtc", "Status", "VersionNumber" },
                values: new object[,]
                {
                    { new Guid("95100000-0000-0000-0000-000000000001"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Small Talk", 1, new Guid("95000000-0000-0000-0000-000000000001"), 10, 3, 5, 3, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000002"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aktivacija predznanja", 1, new Guid("95000000-0000-0000-0000-000000000002"), 15, 5, 10, 3, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000003"), 2, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Pregled domaće zadaće", 3, new Guid("95000000-0000-0000-0000-000000000003"), 20, 5, 10, 3, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000004"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Objašnjenje novog gradiva", 1, new Guid("95000000-0000-0000-0000-000000000004"), 25, 8, 15, 3, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000005"), 2, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Interaktivni zadaci", 2, new Guid("95000000-0000-0000-0000-000000000005"), 30, 10, 20, 3, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000006"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mini provjera razumijevanja", 1, new Guid("95000000-0000-0000-0000-000000000006"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000007"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Završna refleksija", 1, new Guid("95000000-0000-0000-0000-000000000007"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000008"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Speaking", 1, new Guid("95000000-0000-0000-0000-000000000008"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000009"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Listening", 1, new Guid("95000000-0000-0000-0000-000000000009"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000010"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Reading", 1, new Guid("95000000-0000-0000-0000-000000000010"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000011"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Writing", 1, new Guid("95000000-0000-0000-0000-000000000011"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000012"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vocabulary Practice", 1, new Guid("95000000-0000-0000-0000-000000000012"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000013"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Grammar Practice", 1, new Guid("95000000-0000-0000-0000-000000000013"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000014"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Revision", 1, new Guid("95000000-0000-0000-0000-000000000014"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000015"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Domaća zadaća", 1, new Guid("95000000-0000-0000-0000-000000000015"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000016"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Procjena znanja", 1, new Guid("95000000-0000-0000-0000-000000000016"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000017"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rad na pogreškama", 1, new Guid("95000000-0000-0000-0000-000000000017"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000018"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Motivacija učenika", 1, new Guid("95000000-0000-0000-0000-000000000018"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000019"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Projektni zadatak", 1, new Guid("95000000-0000-0000-0000-000000000019"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000020"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Suradnički zadatak", 1, new Guid("95000000-0000-0000-0000-000000000020"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000021"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Edukativna igra", 1, new Guid("95000000-0000-0000-0000-000000000021"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000022"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Simulacija stvarne situacije", 1, new Guid("95000000-0000-0000-0000-000000000022"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000023"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Izazov", 1, new Guid("95000000-0000-0000-0000-000000000023"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000024"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Priča", 1, new Guid("95000000-0000-0000-0000-000000000024"), null, null, null, 3, null, 1, 1 },
                    { new Guid("95100000-0000-0000-0000-000000000025"), 1, new DateTimeOffset(new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Misija", 1, new Guid("95000000-0000-0000-0000-000000000025"), null, null, null, 3, null, 1, 1 }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonPlanVersions_TargetDuration",
                table: "LessonPlanVersions",
                sql: "[TargetDurationMinutes] BETWEEN 5 AND 480");

            migrationBuilder.Sql("""
                UPDATE [LessonPlanVersions]
                SET [UpdatedAtUtc] = [CreatedAtUtc]
                WHERE [UpdatedAtUtc] = '0001-01-01T00:00:00+00:00';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBlocks_Code",
                table: "KnowledgeBlocks",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBlocks_Id_CurrentPublishedVersionId",
                table: "KnowledgeBlocks",
                columns: new[] { "Id", "CurrentPublishedVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBlockVersions_KnowledgeBlockId",
                table: "KnowledgeBlockVersions",
                column: "KnowledgeBlockId",
                unique: true,
                filter: "[Status] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBlockVersions_KnowledgeBlockId_VersionNumber",
                table: "KnowledgeBlockVersions",
                columns: new[] { "KnowledgeBlockId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivities_KnowledgeBlockVersionId",
                table: "LessonActivities",
                column: "KnowledgeBlockVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivities_LessonPlanVersionId_SortOrder",
                table: "LessonActivities",
                columns: new[] { "LessonPlanVersionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonActivityKnowledgeComponents_KnowledgeComponentId",
                table: "LessonActivityKnowledgeComponents",
                column: "KnowledgeComponentId");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBlocks_KnowledgeBlockVersions_Id_CurrentPublishedVersionId",
                table: "KnowledgeBlocks",
                columns: new[] { "Id", "CurrentPublishedVersionId" },
                principalTable: "KnowledgeBlockVersions",
                principalColumns: new[] { "KnowledgeBlockId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE [KnowledgeBlocks]
                SET [CurrentPublishedVersionId] = CASE [Code]
                    WHEN 'KB-001' THEN '95100000-0000-0000-0000-000000000001'
                    WHEN 'KB-002' THEN '95100000-0000-0000-0000-000000000002'
                    WHEN 'KB-003' THEN '95100000-0000-0000-0000-000000000003'
                    WHEN 'KB-004' THEN '95100000-0000-0000-0000-000000000004'
                    WHEN 'KB-005' THEN '95100000-0000-0000-0000-000000000005'
                    ELSE NULL END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_KnowledgeBlockVersions_ProtectSnapshots]
                ON [KnowledgeBlockVersions]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE d.[Status] <> 1 AND (i.[Id] IS NULL OR i.[KnowledgeBlockId] <> d.[KnowledgeBlockId]
                          OR i.[VersionNumber] <> d.[VersionNumber] OR i.[DisplayName] <> d.[DisplayName]
                          OR ISNULL(i.[MinDurationMinutes], -1) <> ISNULL(d.[MinDurationMinutes], -1)
                          OR ISNULL(i.[OptimalDurationMinutes], -1) <> ISNULL(d.[OptimalDurationMinutes], -1)
                          OR ISNULL(i.[MaxDurationMinutes], -1) <> ISNULL(d.[MaxDurationMinutes], -1)
                          OR i.[ParticipantApplicability] <> d.[ParticipantApplicability]
                          OR i.[HomeworkMode] <> d.[HomeworkMode]
                          OR i.[AssessmentCapability] <> d.[AssessmentCapability]
                          OR i.[CreatedAtUtc] <> d.[CreatedAtUtc] OR ISNULL(i.[PublishedAtUtc], '0001-01-01') <> ISNULL(d.[PublishedAtUtc], '0001-01-01'))
                    ) THROW 51310, 'Published and Retired KnowledgeBlockVersion snapshots are immutable.', 1;
                END
                """);

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

            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_LessonActivities_ValidateDraftAndKnowledgeBlock]
                ON [LessonActivities]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1 FROM deleted d JOIN [LessonPlanVersions] p ON p.[Id] = d.[LessonPlanVersionId]
                        WHERE p.[Status] <> 1
                    ) THROW 51311, 'Activities of Ready and Superseded plans are immutable.', 1;

                    IF EXISTS (
                        SELECT 1 FROM inserted i
                        JOIN [LessonPlanVersions] p ON p.[Id] = i.[LessonPlanVersionId]
                        LEFT JOIN [KnowledgeBlockVersions] v ON v.[Id] = i.[KnowledgeBlockVersionId]
                        LEFT JOIN [KnowledgeBlocks] b ON b.[Id] = v.[KnowledgeBlockId]
                        WHERE p.[Status] <> 1 OR (i.[KnowledgeBlockVersionId] IS NOT NULL
                          AND (v.[Status] <> 2 OR b.[Status] <> 1 OR b.[CurrentPublishedVersionId] <> v.[Id]))
                    ) THROW 51311, 'New activities require Draft plans and current Published KnowledgeBlock versions.', 1;
                END
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_LessonActivityKnowledgeComponents_Validate]
                ON [LessonActivityKnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1 FROM deleted d JOIN [LessonActivities] a ON a.[Id] = d.[LessonActivityId]
                        JOIN [LessonPlanVersions] p ON p.[Id] = a.[LessonPlanVersionId]
                        WHERE p.[Status] <> 1
                    ) THROW 51312, 'Activity knowledge mappings of Ready and Superseded plans are immutable.', 1;

                    IF EXISTS (
                        SELECT 1 FROM inserted i JOIN [LessonActivities] a ON a.[Id] = i.[LessonActivityId]
                        JOIN [LessonPlanVersions] p ON p.[Id] = a.[LessonPlanVersionId]
                        LEFT JOIN [LessonPlanVersionKnowledgeComponents] m
                          ON m.[LessonPlanVersionId] = a.[LessonPlanVersionId] AND m.[KnowledgeComponentId] = i.[KnowledgeComponentId]
                        WHERE p.[Status] <> 1 OR m.[KnowledgeComponentId] IS NULL
                    ) THROW 51312, 'Activity mappings must be a subset of Draft plan knowledge mappings.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_KnowledgeBlockVersions_ProtectSnapshots];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_LessonActivities_ValidateDraftAndKnowledgeBlock];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_LessonActivityKnowledgeComponents_Validate];");
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
                          OR i.[CreatedAtUtc] <> d.[CreatedAtUtc])
                    ) THROW 51303, 'Ready and Superseded LessonPlanVersion snapshots are immutable.', 1;
                END
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBlocks_KnowledgeBlockVersions_Id_CurrentPublishedVersionId",
                table: "KnowledgeBlocks");

            migrationBuilder.DropTable(
                name: "LessonActivityKnowledgeComponents");

            migrationBuilder.DropTable(
                name: "LessonActivities");

            migrationBuilder.DropTable(
                name: "KnowledgeBlockVersions");

            migrationBuilder.DropTable(
                name: "KnowledgeBlocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LessonPlanVersions_TargetDuration",
                table: "LessonPlanVersions");

            migrationBuilder.DropColumn(
                name: "TargetDurationMinutes",
                table: "LessonPlanVersions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "LessonPlanVersions");
        }
    }
}
