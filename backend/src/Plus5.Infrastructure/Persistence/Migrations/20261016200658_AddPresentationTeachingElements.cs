using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationTeachingElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_Asset",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationAssets_MediaType",
                table: "PresentationAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationAssets_Size",
                table: "PresentationAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationAssets_Status",
                table: "PresentationAssets");

            migrationBuilder.AddColumn<string>(
                name: "AssetKindCode",
                table: "PresentationAssets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "IMAGE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_Asset",
                table: "PresentationElements",
                sql: "([ElementTypeCode] IN ('IMAGE', 'VIDEO', 'AUDIO') AND [PresentationAssetId] IS NOT NULL) OR ([ElementTypeCode] NOT IN ('IMAGE', 'VIDEO', 'AUDIO') AND [PresentationAssetId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements",
                sql: "[ElementTypeCode] IN ('TEXT', 'IMAGE', 'SHAPE', 'TABLE', 'RULE', 'EXAMPLE', 'VOCABULARY', 'QUESTION', 'VIDEO', 'AUDIO')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationAssets_MediaType",
                table: "PresentationAssets",
                sql: "([AssetKindCode] = 'IMAGE' AND [MediaType] IN ('image/png', 'image/jpeg', 'image/webp')) OR ([AssetKindCode] = 'VIDEO' AND [MediaType] = 'video/mp4') OR ([AssetKindCode] = 'AUDIO' AND [MediaType] = 'audio/mpeg')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationAssets_Size",
                table: "PresentationAssets",
                sql: "[SizeBytes] > 0 AND (([AssetKindCode] = 'IMAGE' AND [SizeBytes] <= 10485760) OR ([AssetKindCode] = 'VIDEO' AND [SizeBytes] <= 262144000) OR ([AssetKindCode] = 'AUDIO' AND [SizeBytes] <= 26214400))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationAssets_Status",
                table: "PresentationAssets",
                sql: "[Status] IN (1, 2)");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_PresentationAssets_ProtectSnapshot]
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

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted asset
                        INNER JOIN [PresentationElements] element
                            ON element.[MaterialVersionId] = asset.[MaterialVersionId]
                           AND element.[PresentationAssetId] = asset.[Id]
                        WHERE asset.[Status] <> 1
                           OR asset.[AssetKindCode] <> element.[ElementTypeCode]
                    )
                    BEGIN
                        THROW 51245, 'Referenced presentation asset must remain Clean and match its element kind.', 1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_PresentationElements_ProtectSnapshot]
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

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted element
                        LEFT JOIN [PresentationAssets] asset
                            ON asset.[MaterialVersionId] = element.[MaterialVersionId]
                           AND asset.[Id] = element.[PresentationAssetId]
                        WHERE element.[ElementTypeCode] IN (N'IMAGE', N'VIDEO', N'AUDIO')
                          AND
                          (
                              asset.[Id] IS NULL
                              OR asset.[Status] <> 1
                              OR asset.[AssetKindCode] <> element.[ElementTypeCode]
                          )
                    )
                    BEGIN
                        THROW 51245, 'Presentation element asset kind, version or Clean status is invalid.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_Asset",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationAssets_MediaType",
                table: "PresentationAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationAssets_Size",
                table: "PresentationAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationAssets_Status",
                table: "PresentationAssets");

            migrationBuilder.DropColumn(
                name: "AssetKindCode",
                table: "PresentationAssets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_Asset",
                table: "PresentationElements",
                sql: "([ElementTypeCode] = 'IMAGE' AND [PresentationAssetId] IS NOT NULL) OR ([ElementTypeCode] <> 'IMAGE' AND [PresentationAssetId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements",
                sql: "[ElementTypeCode] IN ('TEXT', 'IMAGE', 'SHAPE', 'TABLE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationAssets_MediaType",
                table: "PresentationAssets",
                sql: "[MediaType] IN ('image/png', 'image/jpeg', 'image/webp')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationAssets_Size",
                table: "PresentationAssets",
                sql: "[SizeBytes] > 0 AND [SizeBytes] <= 10485760");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationAssets_Status",
                table: "PresentationAssets",
                sql: "[Status] = 1");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_PresentationAssets_ProtectSnapshot]
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
                CREATE OR ALTER TRIGGER [TR_PresentationElements_ProtectSnapshot]
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
    }
}
