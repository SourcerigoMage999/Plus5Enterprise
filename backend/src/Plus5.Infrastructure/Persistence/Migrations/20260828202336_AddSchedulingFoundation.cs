using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core generates inline arrays for migration metadata.

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.UniqueConstraint("AK_Locations_Teacher_Id", x => new { x.TeacherAccountId, x.Id });
                    table.ForeignKey(
                        name: "FK_Locations_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringSessionSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LocalStartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    LocalEndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OnlineMeetingUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PreviousSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    SupersededAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringSessionSeries", x => x.Id);
                    table.UniqueConstraint("AK_RecurringSessionSeries_Teacher_Id", x => new { x.TeacherAccountId, x.Id });
                    table.CheckConstraint("CK_RecurringSessionSeries_Context", "([Kind] = 1 AND [GroupId] IS NOT NULL AND [StudentId] IS NULL) OR ([Kind] = 2 AND [GroupId] IS NULL AND [StudentId] IS NOT NULL)");
                    table.CheckConstraint("CK_RecurringSessionSeries_DateRange", "[EndsOn] >= [StartsOn]");
                    table.CheckConstraint("CK_RecurringSessionSeries_DayOfWeek", "[DayOfWeek] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_RecurringSessionSeries_Kind", "[Kind] IN (1, 2)");
                    table.CheckConstraint("CK_RecurringSessionSeries_Location", "[LocationId] IS NULL OR [OnlineMeetingUrl] IS NULL");
                    table.CheckConstraint("CK_RecurringSessionSeries_Previous", "[PreviousSeriesId] IS NULL OR [PreviousSeriesId] <> [Id]");
                    table.CheckConstraint("CK_RecurringSessionSeries_TimeRange", "[LocalEndTime] > [LocalStartTime]");
                    table.ForeignKey(
                        name: "FK_RecurringSessionSeries_Groups_TeacherAccountId_GroupId",
                        columns: x => new { x.TeacherAccountId, x.GroupId },
                        principalTable: "Groups",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringSessionSeries_Locations_TeacherAccountId_LocationId",
                        columns: x => new { x.TeacherAccountId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringSessionSeries_RecurringSessionSeries_TeacherAccountId_PreviousSeriesId",
                        columns: x => new { x.TeacherAccountId, x.PreviousSeriesId },
                        principalTable: "RecurringSessionSeries",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringSessionSeries_Students_TeacherAccountId_StudentId",
                        columns: x => new { x.TeacherAccountId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringSessionSeries_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryMode = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecurringSessionSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeriesOccurrenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OnlineMeetingUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsSeriesException = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.CheckConstraint("CK_Sessions_Cancellation", "([Status] = 4 AND [CancelledAtUtc] IS NOT NULL) OR ([Status] <> 4 AND [CancelledAtUtc] IS NULL)");
                    table.CheckConstraint("CK_Sessions_Context", "([DeliveryMode] = 1 AND [StudentId] IS NOT NULL AND [GroupId] IS NULL) OR ([DeliveryMode] = 2 AND [StudentId] IS NULL AND [GroupId] IS NOT NULL)");
                    table.CheckConstraint("CK_Sessions_DeliveryMode", "[DeliveryMode] IN (1, 2)");
                    table.CheckConstraint("CK_Sessions_Location", "[LocationId] IS NULL OR [OnlineMeetingUrl] IS NULL");
                    table.CheckConstraint("CK_Sessions_SeriesOccurrence", "([RecurringSessionSeriesId] IS NULL AND [SeriesOccurrenceDate] IS NULL) OR ([RecurringSessionSeriesId] IS NOT NULL AND [SeriesOccurrenceDate] IS NOT NULL)");
                    table.CheckConstraint("CK_Sessions_Status", "[Status] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_Sessions_TimeRange", "[EndsAtUtc] > [StartsAtUtc]");
                    table.ForeignKey(
                        name: "FK_Sessions_Groups_TeacherAccountId_GroupId",
                        columns: x => new { x.TeacherAccountId, x.GroupId },
                        principalTable: "Groups",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Locations_TeacherAccountId_LocationId",
                        columns: x => new { x.TeacherAccountId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_RecurringSessionSeries_TeacherAccountId_RecurringSessionSeriesId",
                        columns: x => new { x.TeacherAccountId, x.RecurringSessionSeriesId },
                        principalTable: "RecurringSessionSeries",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Students_TeacherAccountId_StudentId",
                        columns: x => new { x.TeacherAccountId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "TeacherAccountId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Teacher_Archived",
                table: "Locations",
                columns: new[] { "TeacherAccountId", "ArchivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Locations_Teacher_NormalizedName",
                table: "Locations",
                columns: new[] { "TeacherAccountId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSessionSeries_Group_Active_Day",
                table: "RecurringSessionSeries",
                columns: new[] { "TeacherAccountId", "GroupId", "SupersededAtUtc", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSessionSeries_Student_Active_Day",
                table: "RecurringSessionSeries",
                columns: new[] { "TeacherAccountId", "StudentId", "SupersededAtUtc", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSessionSeries_TeacherAccountId_LocationId",
                table: "RecurringSessionSeries",
                columns: new[] { "TeacherAccountId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSessionSeries_TeacherAccountId_PreviousSeriesId",
                table: "RecurringSessionSeries",
                columns: new[] { "TeacherAccountId", "PreviousSeriesId" });

            migrationBuilder.CreateIndex(
                name: "UX_RecurringSessionSeries_PreviousSeriesId",
                table: "RecurringSessionSeries",
                column: "PreviousSeriesId",
                unique: true,
                filter: "[PreviousSeriesId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Group_Start",
                table: "Sessions",
                columns: new[] { "TeacherAccountId", "GroupId", "StartsAtUtc" },
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Location_Time_Status",
                table: "Sessions",
                columns: new[] { "TeacherAccountId", "LocationId", "StartsAtUtc", "EndsAtUtc", "Status" },
                filter: "[LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Student_Start",
                table: "Sessions",
                columns: new[] { "TeacherAccountId", "StudentId", "StartsAtUtc" },
                filter: "[StudentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Teacher_Time_Status",
                table: "Sessions",
                columns: new[] { "TeacherAccountId", "StartsAtUtc", "EndsAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TeacherAccountId_RecurringSessionSeriesId",
                table: "Sessions",
                columns: new[] { "TeacherAccountId", "RecurringSessionSeriesId" });

            migrationBuilder.CreateIndex(
                name: "UX_Sessions_Series_Occurrence",
                table: "Sessions",
                columns: new[] { "RecurringSessionSeriesId", "SeriesOccurrenceDate" },
                unique: true,
                filter: "[RecurringSessionSeriesId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "RecurringSessionSeries");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}

#pragma warning restore CA1861
