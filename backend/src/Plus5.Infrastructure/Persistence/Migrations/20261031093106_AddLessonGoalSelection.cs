using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generates inline arrays for migration metadata.

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonGoalSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Sessions_TeacherAccountId_Id",
                table: "Sessions",
                columns: new[] { "TeacherAccountId", "Id" });

            migrationBuilder.CreateTable(
                name: "LessonPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LifecycleStatus = table.Column<int>(type: "int", nullable: false),
                    CurrentReadyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlans", x => x.Id);
                    table.UniqueConstraint("AK_LessonPlans_Teacher_Id", x => new { x.TeacherAccountId, x.Id });
                    table.CheckConstraint("CK_LessonPlans_Archive", "([LifecycleStatus] = 1 AND [ArchivedAtUtc] IS NULL) OR ([LifecycleStatus] = 2 AND [ArchivedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_LessonPlans_LifecycleStatus", "[LifecycleStatus] IN (1, 2)");
                    table.CheckConstraint("CK_LessonPlans_Participant", "([StudentId] IS NOT NULL AND [GroupId] IS NULL) OR ([StudentId] IS NULL AND [GroupId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_LessonPlans_Groups_TeacherAccountId_GroupId",
                        columns: x => new { x.TeacherAccountId, x.GroupId },
                        principalTable: "Groups",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonPlans_Sessions_TeacherAccountId_SessionId",
                        columns: x => new { x.TeacherAccountId, x.SessionId },
                        principalTable: "Sessions",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonPlans_Students_TeacherAccountId_StudentId",
                        columns: x => new { x.TeacherAccountId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonPlans_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonPlanVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlanVersions", x => x.Id);
                    table.UniqueConstraint("AK_LessonPlanVersions_Plan_Id", x => new { x.LessonPlanId, x.Id });
                    table.CheckConstraint("CK_LessonPlanVersions_Status", "[Status] IN (1, 2, 3)");
                    table.CheckConstraint("CK_LessonPlanVersions_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_LessonPlanVersions_LessonPlans_LessonPlanId",
                        column: x => x.LessonPlanId,
                        principalTable: "LessonPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonPlanVersionKnowledgeComponents",
                columns: table => new
                {
                    LessonPlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlanVersionKnowledgeComponents", x => new { x.LessonPlanVersionId, x.KnowledgeComponentId });
                    table.ForeignKey(
                        name: "FK_LessonPlanVersionKnowledgeComponents_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonPlanVersionKnowledgeComponents_LessonPlanVersions_LessonPlanVersionId",
                        column: x => x.LessonPlanVersionId,
                        principalTable: "LessonPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_Id_CurrentReadyVersionId",
                table: "LessonPlans",
                columns: new[] { "Id", "CurrentReadyVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_Teacher_Status_Created",
                table: "LessonPlans",
                columns: new[] { "TeacherAccountId", "LifecycleStatus", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_TeacherAccountId_GroupId",
                table: "LessonPlans",
                columns: new[] { "TeacherAccountId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_TeacherAccountId_SessionId",
                table: "LessonPlans",
                columns: new[] { "TeacherAccountId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_TeacherAccountId_StudentId",
                table: "LessonPlans",
                columns: new[] { "TeacherAccountId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlanVersionKnowledgeComponents_ComponentId",
                table: "LessonPlanVersionKnowledgeComponents",
                column: "KnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "UX_LessonPlanVersions_Plan_WorkingStatus",
                table: "LessonPlanVersions",
                columns: new[] { "LessonPlanId", "Status" },
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "UX_LessonPlanVersions_Plan_Version",
                table: "LessonPlanVersions",
                columns: new[] { "LessonPlanId", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonPlans_LessonPlanVersions_Id_CurrentReadyVersionId",
                table: "LessonPlans",
                columns: new[] { "Id", "CurrentReadyVersionId" },
                principalTable: "LessonPlanVersions",
                principalColumns: new[] { "LessonPlanId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_LessonPlans_ValidateContext]
                ON [LessonPlans]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN [Sessions] s ON s.[Id] = i.[SessionId]
                        WHERE i.[SessionId] IS NOT NULL
                          AND (s.[TeacherAccountId] <> i.[TeacherAccountId]
                               OR ISNULL(s.[StudentId], '00000000-0000-0000-0000-000000000000') <> ISNULL(i.[StudentId], '00000000-0000-0000-0000-000000000000')
                               OR ISNULL(s.[GroupId], '00000000-0000-0000-0000-000000000000') <> ISNULL(i.[GroupId], '00000000-0000-0000-0000-000000000000'))
                    ) THROW 51301, 'Lesson plan Session must match the Teacher and participant context.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        LEFT JOIN [LessonPlanVersions] v
                          ON v.[LessonPlanId] = i.[Id] AND v.[Id] = i.[CurrentReadyVersionId]
                        WHERE i.[CurrentReadyVersionId] IS NOT NULL AND (v.[Id] IS NULL OR v.[Status] <> 2)
                    ) THROW 51302, 'Current ready version must be a Ready version of the same LessonPlan.', 1;
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
                        SELECT 1
                        FROM deleted d
                        LEFT JOIN inserted i ON i.[Id] = d.[Id]
                        WHERE d.[Status] <> 1
                          AND (i.[Id] IS NULL
                               OR i.[LessonPlanId] <> d.[LessonPlanId]
                               OR i.[VersionNumber] <> d.[VersionNumber]
                               OR i.[Goal] <> d.[Goal]
                               OR i.[CreatedAtUtc] <> d.[CreatedAtUtc])
                    ) THROW 51303, 'Ready and Superseded LessonPlanVersion snapshots are immutable.', 1;
                END
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_LessonPlanVersionKnowledgeComponents_Validate]
                ON [LessonPlanVersionKnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (
                        SELECT 1
                        FROM deleted d
                        JOIN [LessonPlanVersions] v ON v.[Id] = d.[LessonPlanVersionId]
                        WHERE v.[Status] <> 1
                    ) THROW 51304, 'Knowledge mappings of Ready and Superseded plan versions are immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN [LessonPlanVersions] v ON v.[Id] = i.[LessonPlanVersionId]
                        JOIN [KnowledgeComponents] c ON c.[Id] = i.[KnowledgeComponentId]
                        JOIN [KnowledgeModels] m ON m.[Id] = c.[KnowledgeModelId]
                        WHERE v.[Status] <> 1 OR c.[Status] <> 1 OR m.[Status] <> 2
                    ) THROW 51305, 'New lesson mappings require Draft plans and Active components from Published models.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonPlans_LessonPlanVersions_Id_CurrentReadyVersionId",
                table: "LessonPlans");

            migrationBuilder.DropTable(
                name: "LessonPlanVersionKnowledgeComponents");

            migrationBuilder.DropTable(
                name: "LessonPlanVersions");

            migrationBuilder.DropTable(
                name: "LessonPlans");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Sessions_TeacherAccountId_Id",
                table: "Sessions");
        }
    }
}
