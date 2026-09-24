using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceEventModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvidenceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    SupersedesEvidenceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceEvents", x => x.Id);
                    table.UniqueConstraint("AK_EvidenceEvents_SourceChain_Id", x => new { x.StudentId, x.SourceKind, x.SourceId, x.Id });
                    table.CheckConstraint("CK_EvidenceEvents_Kind", "[Kind] IN (1, 2, 3)");
                    table.CheckConstraint("CK_EvidenceEvents_LifecycleShape", "([Kind] = 1 AND [SupersedesEvidenceEventId] IS NULL AND [ReasonCode] IS NULL) OR ([Kind] IN (2, 3) AND [SupersedesEvidenceEventId] IS NOT NULL AND [ReasonCode] IS NOT NULL)");
                    table.CheckConstraint("CK_EvidenceEvents_NotOwnPredecessor", "[SupersedesEvidenceEventId] IS NULL OR [SupersedesEvidenceEventId] <> [Id]");
                    table.CheckConstraint("CK_EvidenceEvents_ReasonCode", "[ReasonCode] IS NULL OR (LEN(LTRIM(RTRIM([ReasonCode]))) > 0 AND [ReasonCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2)");
                    table.CheckConstraint("CK_EvidenceEvents_SourceId", "[SourceId] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_EvidenceEvents_SourceKind", "LEN(LTRIM(RTRIM([SourceKind]))) > 0 AND [SourceKind] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2");
                    table.CheckConstraint("CK_EvidenceEvents_Utc", "DATEPART(TZOFFSET, [OccurredAtUtc]) = 0 AND DATEPART(TZOFFSET, [RecordedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_EvidenceEvents_EvidenceEvents_StudentId_SourceKind_SourceId_SupersedesEvidenceEventId",
                        columns: x => new { x.StudentId, x.SourceKind, x.SourceId, x.SupersedesEvidenceEventId },
                        principalTable: "EvidenceEvents",
                        principalColumns: new[] { "StudentId", "SourceKind", "SourceId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceEvents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceEventKnowledgeComponents",
                columns: table => new
                {
                    EvidenceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceEventKnowledgeComponents", x => new { x.EvidenceEventId, x.KnowledgeComponentId });
                    table.ForeignKey(
                        name: "FK_EvidenceEventKnowledgeComponents_EvidenceEvents_EvidenceEventId",
                        column: x => x.EvidenceEventId,
                        principalTable: "EvidenceEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceEventKnowledgeComponents_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceEventKnowledgeComponents_ComponentId",
                table: "EvidenceEventKnowledgeComponents",
                column: "KnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceEvents_Student_Occurred_Id",
                table: "EvidenceEvents",
                columns: new[] { "StudentId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceEvents_StudentId_SourceKind_SourceId_SupersedesEvidenceEventId",
                table: "EvidenceEvents",
                columns: new[] { "StudentId", "SourceKind", "SourceId", "SupersedesEvidenceEventId" });

            migrationBuilder.CreateIndex(
                name: "UX_EvidenceEvents_Predecessor_Successor",
                table: "EvidenceEvents",
                column: "SupersedesEvidenceEventId",
                unique: true,
                filter: "[SupersedesEvidenceEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EvidenceEvents_Student_Source_Root",
                table: "EvidenceEvents",
                columns: new[] { "StudentId", "SourceKind", "SourceId" },
                unique: true,
                filter: "[SupersedesEvidenceEventId] IS NULL");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_EvidenceEvents_ValidateAppendOnly]
                ON [EvidenceEvents]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51110, 'Evidence events are append-only and cannot be updated.', 1;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS successor
                        INNER JOIN [EvidenceEvents] AS predecessor
                            ON predecessor.[Id] = successor.[SupersedesEvidenceEventId]
                        WHERE predecessor.[Kind] = 3
                    )
                        THROW 51111, 'An invalidation is terminal and cannot be superseded.', 1;

                    DECLARE @hasCycle bit = 0;

                    ;WITH [EvidenceChain] AS
                    (
                        SELECT
                            insertedEvent.[Id] AS [StartId],
                            insertedEvent.[SupersedesEvidenceEventId] AS [CurrentId]
                        FROM inserted AS insertedEvent
                        WHERE insertedEvent.[SupersedesEvidenceEventId] IS NOT NULL

                        UNION ALL

                        SELECT
                            chain.[StartId],
                            predecessor.[SupersedesEvidenceEventId]
                        FROM [EvidenceChain] AS chain
                        INNER JOIN [EvidenceEvents] AS predecessor
                            ON predecessor.[Id] = chain.[CurrentId]
                        WHERE chain.[CurrentId] <> chain.[StartId]
                            AND predecessor.[SupersedesEvidenceEventId] IS NOT NULL
                    )
                    SELECT TOP (1) @hasCycle = 1
                    FROM [EvidenceChain]
                    WHERE [CurrentId] = [StartId]
                    OPTION (MAXRECURSION 32767);

                    IF @hasCycle = 1
                        THROW 51112, 'An evidence chain cannot contain a cycle.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_EvidenceEvents_BlockDelete]
                ON [EvidenceEvents]
                INSTEAD OF DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51110, 'Evidence events are append-only and cannot be deleted.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_EvidenceEventKnowledgeComponents_ValidateTargets]
                ON [EvidenceEventKnowledgeComponents]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51113, 'Evidence knowledge targets are append-only.', 1;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS target
                        INNER JOIN [EvidenceEvents] AS evidence
                            ON evidence.[Id] = target.[EvidenceEventId]
                        WHERE evidence.[Kind] = 3
                    )
                        THROW 51114, 'Invalidation cannot carry a knowledge target.', 1;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS target
                        INNER JOIN [KnowledgeComponents] AS component
                            ON component.[Id] = target.[KnowledgeComponentId]
                        INNER JOIN [KnowledgeModels] AS model
                            ON model.[Id] = component.[KnowledgeModelId]
                        WHERE model.[Status] = 1
                    )
                        THROW 51115, 'Direct evidence cannot target a draft knowledge model.', 1;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS target
                        WHERE EXISTS
                        (
                            SELECT 1
                            FROM [KnowledgeComponents] AS child
                            WHERE child.[ParentComponentId] = target.[KnowledgeComponentId]
                        )
                    )
                        THROW 51116, 'Direct evidence can target only a leaf knowledge component.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenceEventKnowledgeComponents");

            migrationBuilder.DropTable(
                name: "EvidenceEvents");
        }
    }
}

#pragma warning restore CA1861
