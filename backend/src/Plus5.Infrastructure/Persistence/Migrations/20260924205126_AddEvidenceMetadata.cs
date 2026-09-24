using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssistanceLevel",
                table: "EvidenceEvents",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "EvidenceEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceContext",
                table: "EvidenceEvents",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceType",
                table: "EvidenceEvents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_AssistanceLevel",
                table: "EvidenceEvents",
                sql: "[AssistanceLevel] IS NULL OR [AssistanceLevel] IN (N'Independent', N'MinorAssistance', N'SignificantAssistance', N'NotObserved')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_Difficulty",
                table: "EvidenceEvents",
                sql: "[Difficulty] IS NULL OR [Difficulty] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_EvidenceContext",
                table: "EvidenceEvents",
                sql: "[EvidenceContext] IS NULL OR [EvidenceContext] IN (N'Lesson', N'Homework', N'Assessment', N'IndependentPractice')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_EvidenceType",
                table: "EvidenceEvents",
                sql: "[EvidenceType] IS NULL OR [EvidenceType] IN (N'Recognition', N'Understanding', N'Application', N'Production')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_MetadataShape",
                table: "EvidenceEvents",
                sql: "([Kind] IN (1, 2) AND [Difficulty] IS NOT NULL AND [EvidenceType] IS NOT NULL AND [AssistanceLevel] IS NOT NULL AND [EvidenceContext] IS NOT NULL) OR ([Kind] = 3 AND [Difficulty] IS NULL AND [EvidenceType] IS NULL AND [AssistanceLevel] IS NULL AND [EvidenceContext] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_AssistanceLevel",
                table: "EvidenceEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_Difficulty",
                table: "EvidenceEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_EvidenceContext",
                table: "EvidenceEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_EvidenceType",
                table: "EvidenceEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_MetadataShape",
                table: "EvidenceEvents");

            migrationBuilder.DropColumn(
                name: "AssistanceLevel",
                table: "EvidenceEvents");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "EvidenceEvents");

            migrationBuilder.DropColumn(
                name: "EvidenceContext",
                table: "EvidenceEvents");

            migrationBuilder.DropColumn(
                name: "EvidenceType",
                table: "EvidenceEvents");
        }
    }
}
