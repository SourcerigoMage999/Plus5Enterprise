using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationAiAssistance : Migration
    {
        private static readonly string[] MaterialVersionColumns =
            ["MaterialId", "MaterialVersionId"];
        private static readonly string[] MaterialVersionKeyColumns =
            ["MaterialId", "Id"];
        private static readonly string[] MaterialOwnerColumns =
            ["MaterialId", "TeacherAccountId"];
        private static readonly string[] MaterialOwnerKeyColumns =
            ["Id", "OwnerTeacherId"];
        private static readonly string[] TargetElementColumns =
            ["MaterialVersionId", "TargetElementId"];
        private static readonly string[] TargetSlideColumns =
            ["MaterialVersionId", "TargetSlideId"];
        private static readonly string[] PresentationChildKeyColumns =
            ["MaterialVersionId", "Id"];
        private static readonly string[] TeacherStatusExpiryColumns =
            ["TeacherAccountId", "Status", "ExpiresAtUtc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresentationAiSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresentationDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetSlideId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestionKind = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PromptTemplateVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GeneratedContentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentationAiSuggestions", x => x.Id);
                    table.CheckConstraint("CK_PresentationAiSuggestions_ContentLifecycle", "([Status] = 1 AND [GeneratedContentJson] IS NOT NULL AND ISJSON([GeneratedContentJson]) = 1) OR ([Status] <> 1 AND [GeneratedContentJson] IS NULL)");
                    table.CheckConstraint("CK_PresentationAiSuggestions_DocumentVersion", "[PresentationDocumentId] = [MaterialVersionId]");
                    table.CheckConstraint("CK_PresentationAiSuggestions_Expiry", "[ExpiresAtUtc] > [GeneratedAtUtc]");
                    table.CheckConstraint("CK_PresentationAiSuggestions_Resolution", "([Status] = 1 AND [ResolvedAtUtc] IS NULL) OR ([Status] <> 1 AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedAtUtc] >= [GeneratedAtUtc])");
                    table.CheckConstraint("CK_PresentationAiSuggestions_Status", "[Status] IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_PresentationAiSuggestions_MaterialVersions_MaterialId_MaterialVersionId",
                        columns: x => new { x.MaterialId, x.MaterialVersionId },
                        principalTable: "MaterialVersions",
                        principalColumns: MaterialVersionKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentationAiSuggestions_Materials_MaterialId_TeacherAccountId",
                        columns: x => new { x.MaterialId, x.TeacherAccountId },
                        principalTable: "Materials",
                        principalColumns: MaterialOwnerKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentationAiSuggestions_PresentationDocuments_PresentationDocumentId",
                        column: x => x.PresentationDocumentId,
                        principalTable: "PresentationDocuments",
                        principalColumn: "MaterialVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentationAiSuggestions_PresentationElements_MaterialVersionId_TargetElementId",
                        columns: x => new { x.MaterialVersionId, x.TargetElementId },
                        principalTable: "PresentationElements",
                        principalColumns: PresentationChildKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentationAiSuggestions_PresentationSlides_MaterialVersionId_TargetSlideId",
                        columns: x => new { x.MaterialVersionId, x.TargetSlideId },
                        principalTable: "PresentationSlides",
                        principalColumns: PresentationChildKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PresentationAiSuggestions_MaterialId_MaterialVersionId",
                table: "PresentationAiSuggestions",
                columns: MaterialVersionColumns);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationAiSuggestions_MaterialId_TeacherAccountId",
                table: "PresentationAiSuggestions",
                columns: MaterialOwnerColumns);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationAiSuggestions_MaterialVersionId_TargetElementId",
                table: "PresentationAiSuggestions",
                columns: TargetElementColumns);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationAiSuggestions_MaterialVersionId_TargetSlideId",
                table: "PresentationAiSuggestions",
                columns: TargetSlideColumns);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationAiSuggestions_PresentationDocumentId",
                table: "PresentationAiSuggestions",
                column: "PresentationDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PresentationAiSuggestions_Teacher_Status_Expiry",
                table: "PresentationAiSuggestions",
                columns: TeacherStatusExpiryColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresentationAiSuggestions");
        }
    }
}
