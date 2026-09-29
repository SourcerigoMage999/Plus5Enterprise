using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialStorageFoundation : Migration
    {
        private static readonly string[] MaterialOwnerColumns = ["Id", "OwnerTeacherId"];
        private static readonly string[] FileStatusUpdatedColumns = ["Status", "UpdatedAtUtc"];
        private static readonly string[] MaterialCurrentColumns = ["Id", "CurrentVersionId"];
        private static readonly string[] MaterialOwnerStatusColumns = ["OwnerTeacherId", "ArchivedAtUtc", "Status"];
        private static readonly string[] ShareRecipientColumns = ["SharedWithTeacherId", "Permission", "MaterialId"];
        private static readonly string[] VersionOwnerColumns = ["MaterialId", "CreatedByTeacherId"];
        private static readonly string[] VersionNumberColumns = ["MaterialId", "VersionNumber"];
        private static readonly string[] VersionPrincipalColumns = ["MaterialId", "Id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeclaredMediaType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DeclaredSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ActualSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    StorageProvider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StorageContainer = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScanAttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastScanResultCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    ScannedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialFiles", x => x.Id);
                    table.CheckConstraint("CK_MaterialFiles_ActualSize", "[ActualSizeBytes] IS NULL OR [ActualSizeBytes] = [DeclaredSizeBytes]");
                    table.CheckConstraint("CK_MaterialFiles_DeclaredSize", "[DeclaredSizeBytes] > 0");
                    table.CheckConstraint("CK_MaterialFiles_Format", "[Format] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_MaterialFiles_FormatSize", "([Format] = 1 AND [DeclaredSizeBytes] <= 52428800) OR ([Format] = 2 AND [DeclaredSizeBytes] <= 26214400) OR ([Format] = 3 AND [DeclaredSizeBytes] <= 104857600) OR ([Format] = 4 AND [DeclaredSizeBytes] <= 262144000) OR ([Format] = 5 AND [DeclaredSizeBytes] <= 104857600)");
                    table.CheckConstraint("CK_MaterialFiles_ScanAttempts", "[ScanAttemptCount] >= 0 AND [ScanAttemptCount] <= 3");
                    table.CheckConstraint("CK_MaterialFiles_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
                    table.CheckConstraint("CK_MaterialFiles_UploadedChecksum", "([Status] = 1 AND [ActualSizeBytes] IS NULL AND [Sha256Checksum] IS NULL AND [UploadedAtUtc] IS NULL) OR ([Status] = 6 AND (([ActualSizeBytes] IS NULL AND [Sha256Checksum] IS NULL AND [UploadedAtUtc] IS NULL) OR ([ActualSizeBytes] IS NOT NULL AND [Sha256Checksum] IS NOT NULL AND [UploadedAtUtc] IS NOT NULL))) OR ([Status] NOT IN (1, 6) AND [ActualSizeBytes] IS NOT NULL AND [Sha256Checksum] IS NOT NULL AND [UploadedAtUtc] IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.UniqueConstraint("AK_Materials_Id_OwnerTeacherId", x => new { x.Id, x.OwnerTeacherId });
                    table.CheckConstraint("CK_Materials_ArchivedStatus", "([Status] = 1 AND [ArchivedAtUtc] IS NULL) OR ([Status] = 2 AND [ArchivedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Materials_Status", "[Status] IN (1, 2)");
                    table.CheckConstraint("CK_Materials_Visibility", "[Visibility] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_Materials_UserAccounts_OwnerTeacherId",
                        column: x => x.OwnerTeacherId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialShares",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialShares", x => new { x.MaterialId, x.SharedWithTeacherId });
                    table.CheckConstraint("CK_MaterialShares_Permission", "[Permission] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_MaterialShares_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialShares_UserAccounts_SharedWithTeacherId",
                        column: x => x.SharedWithTeacherId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedByTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    SupersededAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVersions", x => x.Id);
                    table.UniqueConstraint("AK_MaterialVersions_Material_Id", x => new { x.MaterialId, x.Id });
                    table.CheckConstraint("CK_MaterialVersions_LifecycleTimes", "([Status] = 1 AND [ActivatedAtUtc] IS NULL AND [SupersededAtUtc] IS NULL) OR ([Status] = 2 AND [ActivatedAtUtc] IS NOT NULL AND [SupersededAtUtc] IS NULL) OR ([Status] = 3 AND [ActivatedAtUtc] IS NOT NULL AND [SupersededAtUtc] IS NOT NULL AND [SupersededAtUtc] >= [ActivatedAtUtc])");
                    table.CheckConstraint("CK_MaterialVersions_Status", "[Status] IN (1, 2, 3)");
                    table.CheckConstraint("CK_MaterialVersions_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_MaterialVersions_Materials_MaterialId_CreatedByTeacherId",
                        columns: x => new { x.MaterialId, x.CreatedByTeacherId },
                        principalTable: "Materials",
                        principalColumns: MaterialOwnerColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialVersions_UserAccounts_CreatedByTeacherId",
                        column: x => x.CreatedByTeacherId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFiles_MaterialVersionId",
                table: "MaterialFiles",
                column: "MaterialVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFiles_Sha256Checksum",
                table: "MaterialFiles",
                column: "Sha256Checksum");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFiles_Status_UpdatedAtUtc",
                table: "MaterialFiles",
                columns: FileStatusUpdatedColumns);

            migrationBuilder.CreateIndex(
                name: "UX_MaterialFiles_ObjectKey",
                table: "MaterialFiles",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_CurrentVersionId",
                table: "Materials",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Id_CurrentVersionId",
                table: "Materials",
                columns: MaterialCurrentColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Owner_Archived_Status",
                table: "Materials",
                columns: MaterialOwnerStatusColumns);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialShares_Recipient_Permission_Material",
                table: "MaterialShares",
                columns: ShareRecipientColumns);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersions_CreatedByTeacherId",
                table: "MaterialVersions",
                column: "CreatedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersions_MaterialId_CreatedByTeacherId",
                table: "MaterialVersions",
                columns: VersionOwnerColumns);

            migrationBuilder.CreateIndex(
                name: "UX_MaterialVersions_Material_Active",
                table: "MaterialVersions",
                column: "MaterialId",
                unique: true,
                filter: "[Status] = 2");

            migrationBuilder.CreateIndex(
                name: "UX_MaterialVersions_Material_VersionNumber",
                table: "MaterialVersions",
                columns: VersionNumberColumns,
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialFiles_MaterialVersions_MaterialVersionId",
                table: "MaterialFiles",
                column: "MaterialVersionId",
                principalTable: "MaterialVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_MaterialVersions_Id_CurrentVersionId",
                table: "Materials",
                columns: MaterialCurrentColumns,
                principalTable: "MaterialVersions",
                principalColumns: VersionPrincipalColumns,
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_Materials_ValidateCurrentVersion]
                ON [Materials]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE current_row.[OwnerTeacherId] <> previous_row.[OwnerTeacherId])
                        THROW 51220, 'Material ownership is immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS material
                        WHERE material.[CurrentVersionId] IS NULL
                          AND EXISTS (
                              SELECT 1
                              FROM [MaterialVersions] AS version
                              WHERE version.[MaterialId] = material.[Id]
                                AND version.[Status] = 2))
                        THROW 51220, 'An active material version must be the current version.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS material
                        WHERE material.[CurrentVersionId] IS NOT NULL
                          AND NOT EXISTS (
                              SELECT 1
                              FROM [MaterialVersions] AS version
                              WHERE version.[Id] = material.[CurrentVersionId]
                                AND version.[MaterialId] = material.[Id]
                                AND version.[Status] = 2
                                AND (SELECT COUNT_BIG(*)
                                     FROM [MaterialFiles] AS stored_file
                                     WHERE stored_file.[MaterialVersionId] = version.[Id]
                                       AND stored_file.[Status] = 4) = 1))
                        THROW 51220, 'Current material version must be active and have exactly one clean file.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS material
                        WHERE material.[Visibility] = 1
                          AND EXISTS (
                              SELECT 1
                              FROM [MaterialShares] AS share_row
                              WHERE share_row.[MaterialId] = material.[Id]))
                        THROW 51220, 'A private material cannot retain share grants.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialVersions_ProtectLifecycle]
                ON [MaterialVersions]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM deleted AS previous_row
                        LEFT JOIN inserted AS current_row ON current_row.[Id] = previous_row.[Id]
                        WHERE current_row.[Id] IS NULL AND previous_row.[Status] IN (2, 3))
                        THROW 51221, 'Active or superseded material versions cannot be deleted.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE current_row.[MaterialId] <> previous_row.[MaterialId]
                           OR current_row.[VersionNumber] <> previous_row.[VersionNumber]
                           OR current_row.[CreatedByTeacherId] <> previous_row.[CreatedByTeacherId]
                           OR current_row.[CreatedAtUtc] <> previous_row.[CreatedAtUtc])
                        THROW 51221, 'Material version identity is immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE NOT (
                            (previous_row.[Status] = 1 AND current_row.[Status] IN (1, 2))
                            OR (previous_row.[Status] = 2 AND current_row.[Status] = 3)
                            OR (previous_row.[Status] = 3 AND current_row.[Status] = 3)))
                        THROW 51221, 'Material version status transition is not allowed.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE previous_row.[Status] IN (2, 3)
                          AND (current_row.[Title] <> previous_row.[Title]
                            OR ISNULL(current_row.[Description], '') <> ISNULL(previous_row.[Description], '')
                            OR current_row.[MaterialTypeCode] <> previous_row.[MaterialTypeCode]
                            OR ISNULL(current_row.[Subject], '') <> ISNULL(previous_row.[Subject], '')
                            OR ISNULL(current_row.[LanguageCode], '') <> ISNULL(previous_row.[LanguageCode], '')
                            OR current_row.[ActivatedAtUtc] <> previous_row.[ActivatedAtUtc]))
                        THROW 51221, 'Active or superseded material version content is immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE previous_row.[Status] = 1
                          AND current_row.[Status] = 2
                          AND (SELECT COUNT_BIG(*)
                               FROM [MaterialFiles] AS stored_file
                               WHERE stored_file.[MaterialVersionId] = current_row.[Id]
                                 AND stored_file.[Status] = 4) <> 1)
                        THROW 51221, 'A material version requires exactly one clean file before activation.', 1;

                    UPDATE material
                    SET [CurrentVersionId] = current_row.[Id],
                        [UpdatedAtUtc] = CASE
                            WHEN current_row.[ActivatedAtUtc] > material.[UpdatedAtUtc]
                                THEN current_row.[ActivatedAtUtc]
                            ELSE material.[UpdatedAtUtc]
                        END
                    FROM [Materials] AS material
                    INNER JOIN inserted AS current_row ON current_row.[MaterialId] = material.[Id]
                    INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                    WHERE previous_row.[Status] = 1 AND current_row.[Status] = 2;

                    UPDATE material
                    SET [CurrentVersionId] = NULL,
                        [UpdatedAtUtc] = CASE
                            WHEN current_row.[SupersededAtUtc] > material.[UpdatedAtUtc]
                                THEN current_row.[SupersededAtUtc]
                            ELSE material.[UpdatedAtUtc]
                        END
                    FROM [Materials] AS material
                    INNER JOIN inserted AS current_row
                        ON current_row.[MaterialId] = material.[Id]
                       AND current_row.[Id] = material.[CurrentVersionId]
                    INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                    WHERE previous_row.[Status] = 2 AND current_row.[Status] = 3;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialFiles_ProtectLifecycle]
                ON [MaterialFiles]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS stored_file
                        WHERE stored_file.[ObjectKey] NOT LIKE 'materials/%'
                           OR stored_file.[ObjectKey] LIKE '%..%'
                           OR stored_file.[OriginalFileName] LIKE '%/%'
                           OR stored_file.[OriginalFileName] LIKE '%\%')
                        THROW 51222, 'Material file names and object keys must use safe server-generated paths.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE NOT (
                            (previous_row.[Status] = 1 AND current_row.[Status] IN (2, 6))
                            OR (previous_row.[Status] = 2 AND current_row.[Status] IN (3, 6))
                            OR (previous_row.[Status] = 3 AND current_row.[Status] IN (4, 5, 7))
                            OR (previous_row.[Status] = 7 AND current_row.[Status] = 3)
                            OR (previous_row.[Status] IN (4, 5, 6) AND current_row.[Status] = previous_row.[Status])))
                        THROW 51222, 'Material file status transition is not allowed.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE previous_row.[Status] IN (4, 5, 6)
                          AND (current_row.[MaterialVersionId] <> previous_row.[MaterialVersionId]
                            OR current_row.[Format] <> previous_row.[Format]
                            OR current_row.[OriginalFileName] <> previous_row.[OriginalFileName]
                            OR current_row.[DeclaredMediaType] <> previous_row.[DeclaredMediaType]
                            OR current_row.[DeclaredSizeBytes] <> previous_row.[DeclaredSizeBytes]
                            OR ISNULL(current_row.[ActualSizeBytes], -1) <> ISNULL(previous_row.[ActualSizeBytes], -1)
                            OR current_row.[StorageProvider] <> previous_row.[StorageProvider]
                            OR current_row.[StorageContainer] <> previous_row.[StorageContainer]
                            OR current_row.[ObjectKey] <> previous_row.[ObjectKey]
                            OR ISNULL(current_row.[Sha256Checksum], '') <> ISNULL(previous_row.[Sha256Checksum], '')
                            OR current_row.[Status] <> previous_row.[Status]
                            OR current_row.[ScanAttemptCount] <> previous_row.[ScanAttemptCount]
                            OR ISNULL(current_row.[LastScanResultCategory], '') <> ISNULL(previous_row.[LastScanResultCategory], '')
                            OR current_row.[CreatedAtUtc] <> previous_row.[CreatedAtUtc]
                            OR current_row.[UploadedAtUtc] <> previous_row.[UploadedAtUtc]
                            OR ISNULL(current_row.[ScannedAtUtc], '0001-01-01') <> ISNULL(previous_row.[ScannedAtUtc], '0001-01-01')))
                        THROW 51222, 'Terminal material file records are immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS current_row
                        INNER JOIN deleted AS previous_row ON previous_row.[Id] = current_row.[Id]
                        WHERE previous_row.[Status] <> 1
                          AND (current_row.[MaterialVersionId] <> previous_row.[MaterialVersionId]
                            OR current_row.[Format] <> previous_row.[Format]
                            OR current_row.[OriginalFileName] <> previous_row.[OriginalFileName]
                            OR current_row.[DeclaredMediaType] <> previous_row.[DeclaredMediaType]
                            OR current_row.[DeclaredSizeBytes] <> previous_row.[DeclaredSizeBytes]
                            OR current_row.[StorageProvider] <> previous_row.[StorageProvider]
                            OR current_row.[StorageContainer] <> previous_row.[StorageContainer]
                            OR current_row.[ObjectKey] <> previous_row.[ObjectKey]
                            OR current_row.[Sha256Checksum] <> previous_row.[Sha256Checksum]
                            OR current_row.[UploadedAtUtc] <> previous_row.[UploadedAtUtc]))
                        THROW 51222, 'Uploaded material file identity is immutable.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM deleted AS previous_row
                        LEFT JOIN inserted AS current_row ON current_row.[Id] = previous_row.[Id]
                        INNER JOIN [MaterialVersions] AS version ON version.[Id] = previous_row.[MaterialVersionId]
                        WHERE current_row.[Id] IS NULL AND version.[Status] IN (2, 3))
                        THROW 51222, 'Files of active or superseded versions cannot be deleted.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_MaterialShares_RejectOwnerSelfShare]
                ON [MaterialShares]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS share_row
                        INNER JOIN [Materials] AS material ON material.[Id] = share_row.[MaterialId]
                        WHERE share_row.[SharedWithTeacherId] = material.[OwnerTeacherId]
                           OR material.[Visibility] <> 2)
                        THROW 51223, 'A share grant requires a shared material and a non-owner recipient.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_MaterialShares_RejectOwnerSelfShare];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_MaterialFiles_ProtectLifecycle];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_MaterialVersions_ProtectLifecycle];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_Materials_ValidateCurrentVersion];");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_MaterialVersions_Id_CurrentVersionId",
                table: "Materials");

            migrationBuilder.DropTable(
                name: "MaterialFiles");

            migrationBuilder.DropTable(
                name: "MaterialShares");

            migrationBuilder.DropTable(
                name: "MaterialVersions");

            migrationBuilder.DropTable(
                name: "Materials");
        }
    }
}
