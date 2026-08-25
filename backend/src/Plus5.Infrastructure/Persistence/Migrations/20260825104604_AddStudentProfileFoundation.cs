using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentProfileFoundation : Migration
    {
        private static readonly string[] GuardianLookupColumns = ["StudentId", "IsPrimary"];
        private static readonly string[] ProgramOwnerKeyColumns = ["TeacherAccountId", "Id"];
        private static readonly string[] StudentListColumns =
            ["TeacherAccountId", "ArchivedAtUtc", "Status"];
        private static readonly string[] StudentProgramColumns =
            ["TeacherAccountId", "ProgramId"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Programs_Teacher_Id",
                table: "Programs",
                columns: ProgramOwnerKeyColumns);

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolGradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    SchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DeliveryMode = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.CheckConstraint("CK_Students_ArchivedStatus", "[ArchivedAtUtc] IS NULL OR [Status] = 3");
                    table.CheckConstraint("CK_Students_DeliveryMode", "[DeliveryMode] IS NULL OR [DeliveryMode] IN (1, 2)");
                    table.CheckConstraint("CK_Students_Organization", "([ProgramId] IS NULL AND [DeliveryMode] IS NULL) OR ([ProgramId] IS NOT NULL AND [DeliveryMode] IS NOT NULL)");
                    table.CheckConstraint("CK_Students_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Students_Programs_TeacherAccountId_ProgramId",
                        columns: x => new { x.TeacherAccountId, x.ProgramId },
                        principalTable: "Programs",
                        principalColumns: ProgramOwnerKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_SchoolGrades_SchoolGradeId",
                        column: x => x.SchoolGradeId,
                        principalTable: "SchoolGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guardians_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_Student_Primary",
                table: "Guardians",
                columns: GuardianLookupColumns);

            migrationBuilder.CreateIndex(
                name: "UX_Guardians_Student_Primary",
                table: "Guardians",
                column: "StudentId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SchoolGradeId",
                table: "Students",
                column: "SchoolGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Teacher_Archived_Status",
                table: "Students",
                columns: StudentListColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Teacher_ProgramId",
                table: "Students",
                columns: StudentProgramColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guardians");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Programs_Teacher_Id",
                table: "Programs");
        }
    }
}
