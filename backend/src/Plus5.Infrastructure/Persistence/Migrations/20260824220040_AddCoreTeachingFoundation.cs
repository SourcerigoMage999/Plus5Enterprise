using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreTeachingFoundation : Migration
    {
        private static readonly string[] CurriculumIdentityColumns = ["Code", "Version"];
        private static readonly string[] ProficiencyLevelIdentityColumns = ["FrameworkCode", "Code"];
        private static readonly string[] ProficiencyLevelSortColumns = ["FrameworkCode", "SortOrder"];
        private static readonly string[] ProgramIdentityColumns = ["TeacherAccountId", "NormalizedName"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Curricula",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curricula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProficiencyLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProficiencyLevels", x => x.Id);
                    table.CheckConstraint("CK_ProficiencyLevels_SortOrder", "[SortOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Programs_UserAccounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolGrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolGrades", x => x.Id);
                    table.CheckConstraint("CK_SchoolGrades_SortOrder", "[SortOrder] >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "UX_Curricula_Code_Version",
                table: "Curricula",
                columns: CurriculumIdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProficiencyLevels_Framework_SortOrder",
                table: "ProficiencyLevels",
                columns: ProficiencyLevelSortColumns);

            migrationBuilder.CreateIndex(
                name: "UX_ProficiencyLevels_Framework_Code",
                table: "ProficiencyLevels",
                columns: ProficiencyLevelIdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Programs_Teacher_NormalizedName",
                table: "Programs",
                columns: ProgramIdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolGrades_SortOrder",
                table: "SchoolGrades",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "UX_SchoolGrades_Code",
                table: "SchoolGrades",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Curricula");

            migrationBuilder.DropTable(
                name: "ProficiencyLevels");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "SchoolGrades");
        }
    }
}
