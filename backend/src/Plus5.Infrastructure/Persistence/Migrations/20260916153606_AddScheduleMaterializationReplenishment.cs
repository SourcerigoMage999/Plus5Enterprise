using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleMaterializationReplenishment : Migration
    {
        private static readonly string[] MaterializationQueueColumns = ["CreatedAtUtc", "Id"];
        private static readonly string[] MaterializationIssueIdentityColumns =
            ["RecurringSessionSeriesId", "OccurrenceLocalDate", "IssueType"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleMaterializationIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringSessionSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleMaterializationIssues", x => x.Id);
                    table.CheckConstraint("CK_ScheduleMaterializationIssues_AttemptCount", "[AttemptCount] > 0");
                    table.CheckConstraint("CK_ScheduleMaterializationIssues_ObservedRange", "[LastSeenAtUtc] >= [FirstSeenAtUtc]");
                    table.CheckConstraint("CK_ScheduleMaterializationIssues_ResolvedRange", "[ResolvedAtUtc] IS NULL OR [ResolvedAtUtc] >= [LastSeenAtUtc]");
                    table.CheckConstraint("CK_ScheduleMaterializationIssues_Type", "[IssueType] IN (1, 2, 3, 4, 5, 6)");
                    table.ForeignKey(
                        name: "FK_ScheduleMaterializationIssues_RecurringSessionSeries_RecurringSessionSeriesId",
                        column: x => x.RecurringSessionSeriesId,
                        principalTable: "RecurringSessionSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleMaterializationLeases",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleMaterializationLeases", x => x.Name);
                    table.CheckConstraint("CK_ScheduleMaterializationLeases_TimeRange", "[ExpiresAtUtc] >= [UpdatedAtUtc]");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSessionSeries_MaterializationQueue",
                table: "RecurringSessionSeries",
                columns: MaterializationQueueColumns,
                filter: "[EndsOn] IS NULL AND [SupersededAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMaterializationIssues_Unresolved_LastSeen",
                table: "ScheduleMaterializationIssues",
                column: "LastSeenAtUtc",
                filter: "[ResolvedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ScheduleMaterializationIssues_Series_Date_Type",
                table: "ScheduleMaterializationIssues",
                columns: MaterializationIssueIdentityColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleMaterializationIssues");

            migrationBuilder.DropTable(
                name: "ScheduleMaterializationLeases");

            migrationBuilder.DropIndex(
                name: "IX_RecurringSessionSeries_MaterializationQueue",
                table: "RecurringSessionSeries");
        }
    }
}
