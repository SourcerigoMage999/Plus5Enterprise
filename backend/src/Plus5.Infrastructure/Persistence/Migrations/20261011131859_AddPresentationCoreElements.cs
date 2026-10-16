using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationCoreElements : Migration
    {
        private static readonly string[] MaterialVersionIdColumns =
            ["MaterialVersionId", "Id"];
        private static readonly string[] ElementAssetColumns =
            ["MaterialVersionId", "PresentationAssetId"];
        private static readonly string[] ElementSlideColumns =
            ["MaterialVersionId", "PresentationSlideId"];
        private static readonly string[] ElementOrderColumns =
            ["PresentationSlideId", "ZIndex"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresentationAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StorageContainer = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentationAssets", x => x.Id);
                    table.UniqueConstraint("AK_PresentationAssets_MaterialVersion_Id", x => new { x.MaterialVersionId, x.Id });
                    table.CheckConstraint("CK_PresentationAssets_MediaType", "[MediaType] IN ('image/png', 'image/jpeg', 'image/webp')");
                    table.CheckConstraint("CK_PresentationAssets_Size", "[SizeBytes] > 0 AND [SizeBytes] <= 10485760");
                    table.CheckConstraint("CK_PresentationAssets_Status", "[Status] = 1");
                    table.ForeignKey(
                        name: "FK_PresentationAssets_PresentationDocuments_MaterialVersionId",
                        column: x => x.MaterialVersionId,
                        principalTable: "PresentationDocuments",
                        principalColumn: "MaterialVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresentationElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresentationSlideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElementTypeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ElementSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    ZIndex = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PresentationAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentationElements", x => x.Id);
                    table.UniqueConstraint("AK_PresentationElements_MaterialVersion_Id", x => new { x.MaterialVersionId, x.Id });
                    table.CheckConstraint("CK_PresentationElements_Asset", "([ElementTypeCode] = 'IMAGE' AND [PresentationAssetId] IS NOT NULL) OR ([ElementTypeCode] <> 'IMAGE' AND [PresentationAssetId] IS NULL)");
                    table.CheckConstraint("CK_PresentationElements_Bounds", "[X] >= 0 AND [Y] >= 0 AND [Width] > 0 AND [Height] > 0 AND [X] + [Width] <= 1600 AND [Y] + [Height] <= 900");
                    table.CheckConstraint("CK_PresentationElements_Payload", "ISJSON([PayloadJson]) = 1 AND DATALENGTH([PayloadJson]) <= 262144");
                    table.CheckConstraint("CK_PresentationElements_Schema", "[ElementSchemaVersion] = 1");
                    table.CheckConstraint("CK_PresentationElements_Type", "[ElementTypeCode] IN ('TEXT', 'IMAGE', 'SHAPE', 'TABLE')");
                    table.CheckConstraint("CK_PresentationElements_ZIndex", "[ZIndex] >= 0");
                    table.ForeignKey(
                        name: "FK_PresentationElements_PresentationAssets_MaterialVersionId_PresentationAssetId",
                        columns: x => new { x.MaterialVersionId, x.PresentationAssetId },
                        principalTable: "PresentationAssets",
                        principalColumns: MaterialVersionIdColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentationElements_PresentationSlides_MaterialVersionId_PresentationSlideId",
                        columns: x => new { x.MaterialVersionId, x.PresentationSlideId },
                        principalTable: "PresentationSlides",
                        principalColumns: MaterialVersionIdColumns,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PresentationAssets_ObjectKey",
                table: "PresentationAssets",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationElements_MaterialVersionId_PresentationAssetId",
                table: "PresentationElements",
                columns: ElementAssetColumns);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationElements_MaterialVersionId_PresentationSlideId",
                table: "PresentationElements",
                columns: ElementSlideColumns);

            migrationBuilder.CreateIndex(
                name: "UX_PresentationElements_Slide_ZIndex",
                table: "PresentationElements",
                columns: ElementOrderColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_PresentationAssets_ProtectSnapshot]
                ON [PresentationAssets]
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
                        THROW 51243, 'Presentation assets require a Draft PRESENTATION material version.', 1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_PresentationElements_ProtectSnapshot]
                ON [PresentationElements]
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
                        THROW 51244, 'Elements of Active or Superseded presentation versions are immutable.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_PresentationElements_ProtectSnapshot];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_PresentationAssets_ProtectSnapshot];");

            migrationBuilder.DropTable(
                name: "PresentationElements");

            migrationBuilder.DropTable(
                name: "PresentationAssets");
        }
    }
}
