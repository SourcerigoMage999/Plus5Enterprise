using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generates array arguments for migration metadata.

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonSessionRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalendarSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledStartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    ScheduledEndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    ScheduledTimeZoneId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    VoidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastCommittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessions", x => x.Id);
                    table.UniqueConstraint("AK_LessonSessions_Teacher_Id", x => new { x.TeacherAccountId, x.Id });
                    table.CheckConstraint("CK_LessonSessions_CalendarSnapshot", "([CalendarSessionId] IS NULL AND [ScheduledStartsAtUtc] IS NULL AND [ScheduledEndsAtUtc] IS NULL AND [ScheduledTimeZoneId] IS NULL) OR ([CalendarSessionId] IS NOT NULL AND [ScheduledStartsAtUtc] IS NOT NULL AND [ScheduledEndsAtUtc] > [ScheduledStartsAtUtc] AND [ScheduledTimeZoneId] IS NOT NULL)");
                    table.CheckConstraint("CK_LessonSessions_Lifecycle", "([Status] = 1 AND [CompletedAtUtc] IS NULL AND [VoidedAtUtc] IS NULL AND [VoidReason] IS NULL) OR ([Status] = 2 AND [CompletedAtUtc] IS NOT NULL AND [VoidedAtUtc] IS NULL AND [VoidReason] IS NULL) OR ([Status] = 3 AND [CompletedAtUtc] IS NULL AND [VoidedAtUtc] IS NOT NULL AND [VoidReason] IS NOT NULL)");
                    table.CheckConstraint("CK_LessonSessions_Participant", "([StudentId] IS NOT NULL AND [GroupId] IS NULL) OR ([StudentId] IS NULL AND [GroupId] IS NOT NULL)");
                    table.CheckConstraint("CK_LessonSessions_Revision", "[Revision] > 0");
                    table.CheckConstraint("CK_LessonSessions_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_LessonSessions_Groups_TeacherAccountId_GroupId",
                        columns: x => new { x.TeacherAccountId, x.GroupId },
                        principalTable: "Groups",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessions_LessonPlanVersions_LessonPlanId_LessonPlanVersionId",
                        columns: x => new { x.LessonPlanId, x.LessonPlanVersionId },
                        principalTable: "LessonPlanVersions",
                        principalColumns: new[] { "LessonPlanId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessions_LessonPlans_TeacherAccountId_LessonPlanId",
                        columns: x => new { x.TeacherAccountId, x.LessonPlanId },
                        principalTable: "LessonPlans",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessions_Sessions_TeacherAccountId_CalendarSessionId",
                        columns: x => new { x.TeacherAccountId, x.CalendarSessionId },
                        principalTable: "Sessions",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessions_Students_TeacherAccountId_StudentId",
                        columns: x => new { x.TeacherAccountId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessions_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLessonActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    KnowledgeBlockVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    WorkMode = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PurposeDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TeacherInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StudentInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShowTeacherInstructionsOnBoard = table.Column<bool>(type: "bit", nullable: false),
                    ShowStudentInstructionsOnBoard = table.Column<bool>(type: "bit", nullable: false),
                    ShowProgressOnBoard = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessionActivities", x => x.Id);
                    table.UniqueConstraint("AK_LessonSessionActivities_LessonSessionId_Id", x => new { x.LessonSessionId, x.Id });
                    table.CheckConstraint("CK_LessonSessionActivities_Duration", "[DurationMinutes] BETWEEN 1 AND 480");
                    table.CheckConstraint("CK_LessonSessionActivities_Origin", "[Origin] IN (1, 2)");
                    table.CheckConstraint("CK_LessonSessionActivities_Purpose", "[Purpose] BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_LessonSessionActivities_SortOrder", "[SortOrder] >= 0");
                    table.CheckConstraint("CK_LessonSessionActivities_Source", "([Origin] = 1 AND [SourceLessonActivityId] IS NOT NULL) OR ([Origin] = 2 AND [SourceLessonActivityId] IS NULL)");
                    table.CheckConstraint("CK_LessonSessionActivities_Status", "[Status] BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_LessonSessionActivities_WorkMode", "[WorkMode] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_LessonSessionActivities_KnowledgeBlockVersions_KnowledgeBlockVersionId",
                        column: x => x.KnowledgeBlockVersionId,
                        principalTable: "KnowledgeBlockVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessionActivities_LessonActivities_SourceLessonActivityId",
                        column: x => x.SourceLessonActivityId,
                        principalTable: "LessonActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessionActivities_LessonSessions_LessonSessionId",
                        column: x => x.LessonSessionId,
                        principalTable: "LessonSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessionAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessionAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonSessionAuditEvents_LessonSessions_TeacherAccountId_LessonSessionId",
                        columns: x => new { x.TeacherAccountId, x.LessonSessionId },
                        principalTable: "LessonSessions",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessionOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    RequestHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ExpectedRevision = table.Column<long>(type: "bigint", nullable: false),
                    AppliedRevision = table.Column<long>(type: "bigint", nullable: false),
                    CommittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessionOperations", x => x.Id);
                    table.CheckConstraint("CK_LessonSessionOperations_Revision", "[ExpectedRevision] >= 0 AND [AppliedRevision] > 0 AND [AppliedRevision] >= [ExpectedRevision]");
                    table.CheckConstraint("CK_LessonSessionOperations_Type", "[OperationType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_LessonSessionOperations_LessonSessions_TeacherAccountId_LessonSessionId",
                        columns: x => new { x.TeacherAccountId, x.LessonSessionId },
                        principalTable: "LessonSessions",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessionActivityMaterialVersions",
                columns: table => new
                {
                    LessonSessionActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessionActivityMaterialVersions", x => new { x.LessonSessionActivityId, x.MaterialVersionId });
                    table.ForeignKey(
                        name: "FK_LessonSessionActivityMaterialVersions_LessonSessionActivities_LessonSessionActivityId",
                        column: x => x.LessonSessionActivityId,
                        principalTable: "LessonSessionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessionActivityMaterialVersions_MaterialVersions_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessionActivityAssessableTaskVersions",
                columns: table => new
                {
                    LessonSessionActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessableTaskVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessionActivityAssessableTaskVersions", x => new { x.LessonSessionActivityId, x.AssessableTaskVersionId });
                    table.ForeignKey(
                        name: "FK_LessonSessionActivityAssessableTaskVersions_AssessableTaskVersions_MaterialVersionId_AssessableTaskVersionId",
                        columns: x => new { x.MaterialVersionId, x.AssessableTaskVersionId },
                        principalTable: "AssessableTaskVersions",
                        principalColumns: new[] { "MaterialVersionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSessionActivityAssessableTaskVersions_LessonSessionActivityMaterialVersions_LessonSessionActivityId_MaterialVersionId",
                        columns: x => new { x.LessonSessionActivityId, x.MaterialVersionId },
                        principalTable: "LessonSessionActivityMaterialVersions",
                        principalColumns: new[] { "LessonSessionActivityId", "MaterialVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionActivities_KnowledgeBlockVersionId",
                table: "LessonSessionActivities",
                column: "KnowledgeBlockVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionActivities_SourceLessonActivityId",
                table: "LessonSessionActivities",
                column: "SourceLessonActivityId");

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessionActivities_Session_Order",
                table: "LessonSessionActivities",
                columns: new[] { "LessonSessionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessionActivities_Session_Source",
                table: "LessonSessionActivities",
                columns: new[] { "LessonSessionId", "SourceLessonActivityId" },
                unique: true,
                filter: "[SourceLessonActivityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionActivityAssessableTaskVersions_LessonSessionActivityId_MaterialVersionId",
                table: "LessonSessionActivityAssessableTaskVersions",
                columns: new[] { "LessonSessionActivityId", "MaterialVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionActivityAssessableTaskVersions_MaterialVersionId_AssessableTaskVersionId",
                table: "LessonSessionActivityAssessableTaskVersions",
                columns: new[] { "MaterialVersionId", "AssessableTaskVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionActivityMaterialVersions_MaterialVersionId",
                table: "LessonSessionActivityMaterialVersions",
                column: "MaterialVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionAuditEvents_LessonSessionId_OccurredAtUtc_Id",
                table: "LessonSessionAuditEvents",
                columns: new[] { "LessonSessionId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionAuditEvents_TeacherAccountId_LessonSessionId",
                table: "LessonSessionAuditEvents",
                columns: new[] { "TeacherAccountId", "LessonSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionOperations_TeacherAccountId_LessonSessionId",
                table: "LessonSessionOperations",
                columns: new[] { "TeacherAccountId", "LessonSessionId" });

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessionOperations_Session_Operation",
                table: "LessonSessionOperations",
                columns: new[] { "LessonSessionId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessionOperations_Teacher_Operation",
                table: "LessonSessionOperations",
                columns: new[] { "TeacherAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_LessonPlanId_LessonPlanVersionId",
                table: "LessonSessions",
                columns: new[] { "LessonPlanId", "LessonPlanVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_Teacher_Status_Committed",
                table: "LessonSessions",
                columns: new[] { "TeacherAccountId", "Status", "LastCommittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_TeacherAccountId_CalendarSessionId",
                table: "LessonSessions",
                columns: new[] { "TeacherAccountId", "CalendarSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_TeacherAccountId_GroupId",
                table: "LessonSessions",
                columns: new[] { "TeacherAccountId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_TeacherAccountId_StudentId",
                table: "LessonSessions",
                columns: new[] { "TeacherAccountId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessions_Calendar_NonVoided",
                table: "LessonSessions",
                column: "CalendarSessionId",
                unique: true,
                filter: "[CalendarSessionId] IS NOT NULL AND [Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessions_Unscheduled_Group_Active",
                table: "LessonSessions",
                columns: new[] { "TeacherAccountId", "LessonPlanId", "GroupId" },
                unique: true,
                filter: "[CalendarSessionId] IS NULL AND [Status] = 1 AND [GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_LessonSessions_Unscheduled_Student_Active",
                table: "LessonSessions",
                columns: new[] { "TeacherAccountId", "LessonPlanId", "StudentId" },
                unique: true,
                filter: "[CalendarSessionId] IS NULL AND [Status] = 1 AND [StudentId] IS NOT NULL");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonSessions_ProtectLifecycle]
                ON [dbo].[LessonSessions]
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN deleted d ON d.Id = i.Id
                        WHERE i.TeacherAccountId <> d.TeacherAccountId
                           OR i.LessonPlanId <> d.LessonPlanId
                           OR i.LessonPlanVersionId <> d.LessonPlanVersionId
                           OR ISNULL(i.CalendarSessionId, '00000000-0000-0000-0000-000000000000') <> ISNULL(d.CalendarSessionId, '00000000-0000-0000-0000-000000000000')
                           OR ISNULL(i.StudentId, '00000000-0000-0000-0000-000000000000') <> ISNULL(d.StudentId, '00000000-0000-0000-0000-000000000000')
                           OR ISNULL(i.GroupId, '00000000-0000-0000-0000-000000000000') <> ISNULL(d.GroupId, '00000000-0000-0000-0000-000000000000')
                           OR ISNULL(i.ScheduledStartsAtUtc, '0001-01-01') <> ISNULL(d.ScheduledStartsAtUtc, '0001-01-01')
                           OR ISNULL(i.ScheduledEndsAtUtc, '0001-01-01') <> ISNULL(d.ScheduledEndsAtUtc, '0001-01-01')
                           OR ISNULL(i.ScheduledTimeZoneId, N'') <> ISNULL(d.ScheduledTimeZoneId, N'')
                           OR i.StartedAtUtc <> d.StartedAtUtc
                           OR i.Revision <> d.Revision + 1
                           OR d.Status IN (2, 3)
                           OR (i.Status <> d.Status AND NOT (d.Status = 1 AND i.Status IN (2, 3)))
                    )
                        THROW 51201, 'LessonSession identity, anchor, snapshot, lifecycle, and revision are protected.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonSessionActivities_ProtectTerminal]
                ON [dbo].[LessonSessionActivities]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id = d.Id WHERE i.Id IS NULL)
                        THROW 51202, 'LessonSession activities are historical runtime records and cannot be deleted.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN [dbo].[LessonSessions] s ON s.Id = i.LessonSessionId
                        WHERE s.Status <> 1
                    )
                        THROW 51203, 'LessonSession activities can change only while the session is InProgress.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN deleted d ON d.Id = i.Id
                        WHERE i.LessonSessionId <> d.LessonSessionId
                           OR ISNULL(i.SourceLessonActivityId, '00000000-0000-0000-0000-000000000000') <> ISNULL(d.SourceLessonActivityId, '00000000-0000-0000-0000-000000000000')
                           OR i.Origin <> d.Origin
                           OR ISNULL(i.KnowledgeBlockVersionId, '00000000-0000-0000-0000-000000000000') <> ISNULL(d.KnowledgeBlockVersionId, '00000000-0000-0000-0000-000000000000')
                           OR i.Name <> d.Name
                           OR i.Purpose <> d.Purpose
                           OR i.DurationMinutes <> d.DurationMinutes
                           OR i.WorkMode <> d.WorkMode
                           OR ISNULL(i.Description, N'') <> ISNULL(d.Description, N'')
                           OR ISNULL(i.PurposeDescription, N'') <> ISNULL(d.PurposeDescription, N'')
                           OR ISNULL(i.TeacherInstructions, N'') <> ISNULL(d.TeacherInstructions, N'')
                           OR ISNULL(i.StudentInstructions, N'') <> ISNULL(d.StudentInstructions, N'')
                           OR ISNULL(i.Notes, N'') <> ISNULL(d.Notes, N'')
                           OR i.ShowTeacherInstructionsOnBoard <> d.ShowTeacherInstructionsOnBoard
                           OR i.ShowStudentInstructionsOnBoard <> d.ShowStudentInstructionsOnBoard
                           OR i.ShowProgressOnBoard <> d.ShowProgressOnBoard
                    )
                        THROW 51204, 'LessonSession activity source snapshots are immutable.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonSessionActivityMaterialVersions_Immutable]
                ON [dbo].[LessonSessionActivityMaterialVersions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51205, 'LessonSession material references are immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN [dbo].[LessonSessionActivities] a ON a.Id = i.LessonSessionActivityId
                        INNER JOIN [dbo].[LessonSessions] s ON s.Id = a.LessonSessionId
                        WHERE s.Status <> 1
                    )
                        THROW 51206, 'LessonSession material references can be added only while the session is InProgress.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonSessionActivityAssessableTaskVersions_Immutable]
                ON [dbo].[LessonSessionActivityAssessableTaskVersions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51207, 'LessonSession assessable-task references are immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN [dbo].[LessonSessionActivities] a ON a.Id = i.LessonSessionActivityId
                        INNER JOIN [dbo].[LessonSessions] s ON s.Id = a.LessonSessionId
                        WHERE s.Status <> 1
                    )
                        THROW 51208, 'LessonSession assessable-task references can be added only while the session is InProgress.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonSessionOperations_Immutable]
                ON [dbo].[LessonSessionOperations]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51209, 'LessonSession operation records are append-only.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_LessonSessionAuditEvents_AppendOnly]
                ON [dbo].[LessonSessionAuditEvents]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51210, 'LessonSession audit events are append-only.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonSessionActivityAssessableTaskVersions");

            migrationBuilder.DropTable(
                name: "LessonSessionAuditEvents");

            migrationBuilder.DropTable(
                name: "LessonSessionOperations");

            migrationBuilder.DropTable(
                name: "LessonSessionActivityMaterialVersions");

            migrationBuilder.DropTable(
                name: "LessonSessionActivities");

            migrationBuilder.DropTable(
                name: "LessonSessions");
        }
    }
}
