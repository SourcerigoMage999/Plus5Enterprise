using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationSlideDocumentModel : Migration
    {
        private static readonly string[] PresentationSlideOrderColumns =
            ["MaterialVersionId", "SortOrder"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresentationDocuments",
                columns: table => new
                {
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    CanvasWidth = table.Column<int>(type: "int", nullable: false),
                    CanvasHeight = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentationDocuments", x => x.MaterialVersionId);
                    table.CheckConstraint("CK_PresentationDocuments_Canvas", "[CanvasWidth] = 1600 AND [CanvasHeight] = 900");
                    table.CheckConstraint("CK_PresentationDocuments_SchemaVersion", "[DocumentSchemaVersion] > 0");
                    table.ForeignKey(
                        name: "FK_PresentationDocuments_MaterialVersions_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresentationSlides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentationSlides", x => x.Id);
                    table.UniqueConstraint("AK_PresentationSlides_MaterialVersion_Id", x => new { x.MaterialVersionId, x.Id });
                    table.CheckConstraint("CK_PresentationSlides_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_PresentationSlides_PresentationDocuments_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "PresentationDocuments",
                        principalColumn: "MaterialVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PresentationSlides_Document_SortOrder",
                table: "PresentationSlides",
                columns: PresentationSlideOrderColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_PresentationDocuments_ProtectSnapshot]
                ON [PresentationDocuments]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM
                        (
                            SELECT [MaterialVersionId] FROM inserted
                            UNION
                            SELECT [MaterialVersionId] FROM deleted
                        ) changed
                        LEFT JOIN [MaterialVersions] version
                            ON version.[Id] = changed.[MaterialVersionId]
                        WHERE version.[Id] IS NULL
                           OR version.[Status] <> 1
                           OR version.[MaterialTypeCode] <> N'PRESENTATION'
                    )
                    BEGIN
                        THROW 51241, 'Presentation document requires a Draft PRESENTATION material version.', 1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_PresentationSlides_ProtectSnapshot]
                ON [PresentationSlides]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM
                        (
                            SELECT [MaterialVersionId] FROM inserted
                            UNION
                            SELECT [MaterialVersionId] FROM deleted
                        ) changed
                        LEFT JOIN [MaterialVersions] version
                            ON version.[Id] = changed.[MaterialVersionId]
                        WHERE version.[Id] IS NULL
                           OR version.[Status] <> 1
                           OR version.[MaterialTypeCode] <> N'PRESENTATION'
                    )
                    BEGIN
                        THROW 51242, 'Slides of Active or Superseded presentation versions are immutable.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_PresentationSlides_ProtectSnapshot];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_PresentationDocuments_ProtectSnapshot];");

            migrationBuilder.DropTable(
                name: "PresentationSlides");

            migrationBuilder.DropTable(
                name: "PresentationDocuments");
        }
    }
}
