using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMasteryReadinessV1 : Migration
    {
        private static readonly string[] StudentCalculationIndexColumns =
            ["StudentId", "CalculatedAtUtc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_MetadataShape",
                table: "EvidenceEvents");

            migrationBuilder.AddColumn<decimal>(
                name: "PerformanceScore",
                table: "EvidenceEvents",
                type: "decimal(9,8)",
                precision: 9,
                scale: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurriculumOutcomeReadinessEstimates",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,8)", precision: 9, scale: 8, nullable: true),
                    Confidence = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    EffectiveEvidenceWeight = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumOutcomeReadinessEstimates", x => new { x.StudentId, x.CurriculumOutcomeId });
                    table.CheckConstraint("CK_CurriculumOutcomeReadinessEstimates_Confidence", "[Confidence] IN (N'NoData', N'VeryLow', N'Low', N'Medium', N'High')");
                    table.CheckConstraint("CK_CurriculumOutcomeReadinessEstimates_Evidence", "[EvidenceCount] >= 0 AND [EffectiveEvidenceWeight] >= 0");
                    table.CheckConstraint("CK_CurriculumOutcomeReadinessEstimates_NoDataShape", "([Score] IS NULL AND [Confidence] = N'NoData' AND [Readiness] = N'InsufficientData' AND [EvidenceCount] = 0 AND [EffectiveEvidenceWeight] = 0) OR ([Score] IS NOT NULL AND [Confidence] <> N'NoData')");
                    table.CheckConstraint("CK_CurriculumOutcomeReadinessEstimates_Readiness", "[Readiness] IN (N'InsufficientData', N'NeedsWork', N'Developing', N'Ready', N'Strong')");
                    table.CheckConstraint("CK_CurriculumOutcomeReadinessEstimates_Score", "[Score] IS NULL OR [Score] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_CurriculumOutcomeReadinessEstimates_Utc", "DATEPART(TZOFFSET, [CalculatedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomeReadinessEstimates_CurriculumOutcomes_CurriculumOutcomeId",
                        column: x => x.CurriculumOutcomeId,
                        principalTable: "CurriculumOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumOutcomeReadinessEstimates_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeAreaReadinessEstimates",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,8)", precision: 9, scale: 8, nullable: true),
                    Confidence = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    EffectiveEvidenceWeight = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeAreaReadinessEstimates", x => new { x.StudentId, x.KnowledgeAreaId });
                    table.CheckConstraint("CK_KnowledgeAreaReadinessEstimates_Confidence", "[Confidence] IN (N'NoData', N'VeryLow', N'Low', N'Medium', N'High')");
                    table.CheckConstraint("CK_KnowledgeAreaReadinessEstimates_Evidence", "[EvidenceCount] >= 0 AND [EffectiveEvidenceWeight] >= 0");
                    table.CheckConstraint("CK_KnowledgeAreaReadinessEstimates_NoDataShape", "([Score] IS NULL AND [Confidence] = N'NoData' AND [Readiness] = N'InsufficientData' AND [EvidenceCount] = 0 AND [EffectiveEvidenceWeight] = 0) OR ([Score] IS NOT NULL AND [Confidence] <> N'NoData')");
                    table.CheckConstraint("CK_KnowledgeAreaReadinessEstimates_Readiness", "[Readiness] IN (N'InsufficientData', N'NeedsWork', N'Developing', N'Ready', N'Strong')");
                    table.CheckConstraint("CK_KnowledgeAreaReadinessEstimates_Score", "[Score] IS NULL OR [Score] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_KnowledgeAreaReadinessEstimates_Utc", "DATEPART(TZOFFSET, [CalculatedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_KnowledgeAreaReadinessEstimates_KnowledgeAreas_KnowledgeAreaId",
                        column: x => x.KnowledgeAreaId,
                        principalTable: "KnowledgeAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeAreaReadinessEstimates_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasteryEstimates",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,8)", precision: 9, scale: 8, nullable: true),
                    Confidence = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    EffectiveEvidenceWeight = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasteryEstimates", x => new { x.StudentId, x.KnowledgeComponentId });
                    table.CheckConstraint("CK_MasteryEstimates_Confidence", "[Confidence] IN (N'NoData', N'VeryLow', N'Low', N'Medium', N'High')");
                    table.CheckConstraint("CK_MasteryEstimates_Evidence", "[EvidenceCount] >= 0 AND [EffectiveEvidenceWeight] >= 0");
                    table.CheckConstraint("CK_MasteryEstimates_NoDataShape", "([Score] IS NULL AND [Confidence] = N'NoData' AND [Readiness] = N'InsufficientData' AND [EvidenceCount] = 0 AND [EffectiveEvidenceWeight] = 0) OR ([Score] IS NOT NULL AND [Confidence] <> N'NoData')");
                    table.CheckConstraint("CK_MasteryEstimates_Readiness", "[Readiness] IN (N'InsufficientData', N'NeedsWork', N'Developing', N'Ready', N'Strong')");
                    table.CheckConstraint("CK_MasteryEstimates_Score", "[Score] IS NULL OR [Score] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_MasteryEstimates_Utc", "DATEPART(TZOFFSET, [CalculatedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_MasteryEstimates_KnowledgeComponents_KnowledgeComponentId",
                        column: x => x.KnowledgeComponentId,
                        principalTable: "KnowledgeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasteryEstimates_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_MetadataShape",
                table: "EvidenceEvents",
                sql: "([Kind] IN (1, 2) AND [PerformanceScore] IS NOT NULL AND [Difficulty] IS NOT NULL AND [EvidenceType] IS NOT NULL AND [AssistanceLevel] IS NOT NULL AND [EvidenceContext] IS NOT NULL) OR ([Kind] = 3 AND [PerformanceScore] IS NULL AND [Difficulty] IS NULL AND [EvidenceType] IS NULL AND [AssistanceLevel] IS NULL AND [EvidenceContext] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_PerformanceScore",
                table: "EvidenceEvents",
                sql: "[PerformanceScore] IS NULL OR [PerformanceScore] BETWEEN 0 AND 1");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumOutcomeReadinessEstimates_CurriculumOutcomeId",
                table: "CurriculumOutcomeReadinessEstimates",
                column: "CurriculumOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumOutcomeReadinessEstimates_StudentId_CalculatedAtUtc",
                table: "CurriculumOutcomeReadinessEstimates",
                columns: StudentCalculationIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeAreaReadinessEstimates_KnowledgeAreaId",
                table: "KnowledgeAreaReadinessEstimates",
                column: "KnowledgeAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeAreaReadinessEstimates_StudentId_CalculatedAtUtc",
                table: "KnowledgeAreaReadinessEstimates",
                columns: StudentCalculationIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_MasteryEstimates_KnowledgeComponentId",
                table: "MasteryEstimates",
                column: "KnowledgeComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_MasteryEstimates_StudentId_CalculatedAtUtc",
                table: "MasteryEstimates",
                columns: StudentCalculationIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumOutcomeReadinessEstimates");

            migrationBuilder.DropTable(
                name: "KnowledgeAreaReadinessEstimates");

            migrationBuilder.DropTable(
                name: "MasteryEstimates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_MetadataShape",
                table: "EvidenceEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceEvents_PerformanceScore",
                table: "EvidenceEvents");

            migrationBuilder.DropColumn(
                name: "PerformanceScore",
                table: "EvidenceEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceEvents_MetadataShape",
                table: "EvidenceEvents",
                sql: "([Kind] IN (1, 2) AND [Difficulty] IS NOT NULL AND [EvidenceType] IS NOT NULL AND [AssistanceLevel] IS NOT NULL AND [EvidenceContext] IS NOT NULL) OR ([Kind] = 3 AND [Difficulty] IS NULL AND [EvidenceType] IS NULL AND [AssistanceLevel] IS NULL AND [EvidenceContext] IS NULL)");
        }
    }
}
