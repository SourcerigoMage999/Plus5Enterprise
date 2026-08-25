using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupFoundation : Migration
    {
        private static readonly string[] TeacherEntityKeyColumns = ["TeacherAccountId", "Id"];
        private static readonly string[] TeacherProgramColumns = ["TeacherAccountId", "ProgramId"];
        private static readonly string[] TeacherGroupColumns = ["TeacherAccountId", "GroupId"];
        private static readonly string[] TeacherStudentColumns = ["TeacherAccountId", "StudentId"];
        private static readonly string[] GroupMembershipLookupColumns = ["GroupId", "LeftAtUtc"];
        private static readonly string[] GroupMembershipIdentityColumns = ["GroupId", "StudentId", "JoinedAtUtc"];
        private static readonly string[] GroupListColumns = ["TeacherAccountId", "ArchivedAtUtc", "Status"];
        private static readonly string[] GroupNameColumns = ["TeacherAccountId", "NormalizedName"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Students_Teacher_Id",
                table: "Students",
                columns: TeacherEntityKeyColumns);

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolGradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
#pragma warning disable CA1861 // EF requires an anonymous property-access expression here.
                    table.UniqueConstraint("AK_Groups_Teacher_Id", x => new { x.TeacherAccountId, x.Id });
#pragma warning restore CA1861
                    table.CheckConstraint("CK_Groups_ArchivedStatus", "[ArchivedAtUtc] IS NULL OR [Status] = 3");
                    table.CheckConstraint("CK_Groups_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_Groups_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Groups_Programs_TeacherAccountId_ProgramId",
                        columns: x => new { x.TeacherAccountId, x.ProgramId },
                        principalTable: "Programs",
                        principalColumns: TeacherEntityKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Groups_SchoolGrades_SchoolGradeId",
                        column: x => x.SchoolGradeId,
                        principalTable: "SchoolGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Groups_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    LeftAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberships", x => x.Id);
                    table.CheckConstraint("CK_GroupMemberships_Validity", "[LeftAtUtc] IS NULL OR [LeftAtUtc] >= [JoinedAtUtc]");
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Groups_TeacherAccountId_GroupId",
                        columns: x => new { x.TeacherAccountId, x.GroupId },
                        principalTable: "Groups",
                        principalColumns: TeacherEntityKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Students_TeacherAccountId_StudentId",
                        columns: x => new { x.TeacherAccountId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: TeacherEntityKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_Group_LeftAtUtc",
                table: "GroupMemberships",
                columns: GroupMembershipLookupColumns);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_TeacherAccountId_GroupId",
                table: "GroupMemberships",
                columns: TeacherGroupColumns);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_TeacherAccountId_StudentId",
                table: "GroupMemberships",
                columns: TeacherStudentColumns);

            migrationBuilder.CreateIndex(
                name: "UX_GroupMemberships_Group_Student_JoinedAtUtc",
                table: "GroupMemberships",
                columns: GroupMembershipIdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_GroupMemberships_Student_Active",
                table: "GroupMemberships",
                column: "StudentId",
                unique: true,
                filter: "[LeftAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SchoolGradeId",
                table: "Groups",
                column: "SchoolGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Teacher_Archived_Status",
                table: "Groups",
                columns: GroupListColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Teacher_ProgramId",
                table: "Groups",
                columns: TeacherProgramColumns);

            migrationBuilder.CreateIndex(
                name: "UX_Groups_Teacher_NormalizedName",
                table: "Groups",
                columns: GroupNameColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMemberships");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Students_Teacher_Id",
                table: "Students");
        }
    }
}
