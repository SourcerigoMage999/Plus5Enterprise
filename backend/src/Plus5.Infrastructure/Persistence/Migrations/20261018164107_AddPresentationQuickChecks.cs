using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationQuickChecks : Migration
    {
        private static readonly string[] ElementTaskVersionColumns =
            ["MaterialVersionId", "AssessableTaskVersionId"];
        private static readonly string[] TaskVersionKeyColumns =
            ["MaterialVersionId", "Id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements");

            migrationBuilder.AddColumn<Guid>(
                name: "AssessableTaskVersionId",
                table: "PresentationElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthoringPayloadJson",
                table: "AssessableTaskVersions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PresentationElements_MaterialVersionId_AssessableTaskVersionId",
                table: "PresentationElements",
                columns: ElementTaskVersionColumns);

            migrationBuilder.CreateIndex(
                name: "UX_PresentationElements_AssessableTaskVersion",
                table: "PresentationElements",
                column: "AssessableTaskVersionId",
                unique: true,
                filter: "[AssessableTaskVersionId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_AssessableTask",
                table: "PresentationElements",
                sql: "([ElementTypeCode] = 'QUICK_CHECK' AND [AssessableTaskVersionId] IS NOT NULL) OR ([ElementTypeCode] <> 'QUICK_CHECK' AND [AssessableTaskVersionId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements",
                sql: "[ElementTypeCode] IN ('TEXT', 'IMAGE', 'SHAPE', 'TABLE', 'RULE', 'EXAMPLE', 'VOCABULARY', 'QUESTION', 'VIDEO', 'AUDIO', 'QUICK_CHECK')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessableTaskVersions_AuthoringPayload",
                table: "AssessableTaskVersions",
                sql: "([TaskTypeCode] <> 'SINGLE_CHOICE_QUICK_CHECK' AND [AuthoringPayloadJson] IS NULL) OR ([TaskTypeCode] = 'SINGLE_CHOICE_QUICK_CHECK' AND [AuthoringPayloadJson] IS NOT NULL AND ISJSON([AuthoringPayloadJson]) = 1 AND DATALENGTH([AuthoringPayloadJson]) <= 16384)");

            migrationBuilder.AddForeignKey(
                name: "FK_PresentationElements_AssessableTaskVersions_MaterialVersionId_AssessableTaskVersionId",
                table: "PresentationElements",
                columns: ElementTaskVersionColumns,
                principalTable: "AssessableTaskVersions",
                principalColumns: TaskVersionKeyColumns,
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_AssessableTaskVersions_ProtectSnapshot]
                ON [AssessableTaskVersions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS task_version
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS task_version
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                        THROW 51231, 'Assessable task metadata is mutable only within a draft material version.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted
                        WHERE [TaskTypeCode] = ''
                           OR [TaskTypeCode] COLLATE Latin1_General_100_BIN2
                              LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2
                           OR DATALENGTH([Prompt]) = 0
                           OR ([CorrectAnswer] IS NOT NULL AND DATALENGTH([CorrectAnswer]) = 0)
                           OR ([EvaluationCriterion] IS NOT NULL AND DATALENGTH([EvaluationCriterion]) = 0))
                        THROW 51231, 'Assessable task text and type code must use canonical values.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS task_version
                        WHERE task_version.[TaskTypeCode] = N'SINGLE_CHOICE_QUICK_CHECK'
                          AND (
                              task_version.[MaxPoints] <> 1
                              OR task_version.[EvaluationCriterion] IS NOT NULL
                              OR task_version.[CorrectAnswer] IS NULL
                              OR TRY_CONVERT(int, JSON_VALUE(task_version.[AuthoringPayloadJson], '$.schemaVersion')) <> 1
                              OR JSON_QUERY(task_version.[AuthoringPayloadJson], '$.options') IS NULL
                              OR (SELECT COUNT(*) FROM OPENJSON(task_version.[AuthoringPayloadJson], '$.options')) NOT BETWEEN 2 AND 8
                              OR (SELECT COUNT(*) FROM OPENJSON(task_version.[AuthoringPayloadJson])) <> 2
                              OR EXISTS (
                                  SELECT 1
                                  FROM OPENJSON(task_version.[AuthoringPayloadJson]) AS root_property
                                  WHERE root_property.[key] NOT IN (N'schemaVersion', N'options'))
                              OR EXISTS (
                                  SELECT 1
                                  FROM OPENJSON(task_version.[AuthoringPayloadJson], '$.options') AS option_row
                                  WHERE option_row.[type] <> 5
                                     OR TRY_CONVERT(int, option_row.[key]) IS NULL
                                     OR JSON_VALUE(option_row.[value], '$.key')
                                        <> NCHAR(65 + TRY_CONVERT(int, option_row.[key]))
                                     OR JSON_VALUE(option_row.[value], '$.text') IS NULL
                                     OR DATALENGTH(JSON_VALUE(option_row.[value], '$.text')) = 0
                                     OR DATALENGTH(JSON_VALUE(option_row.[value], '$.text')) > 2000
                                     OR (SELECT COUNT(*) FROM OPENJSON(option_row.[value])) <> 2
                                     OR EXISTS (
                                         SELECT 1
                                         FROM OPENJSON(option_row.[value]) AS option_property
                                         WHERE option_property.[key] NOT IN (N'key', N'text')
                                            OR option_property.[type] <> 1))
                              OR NOT EXISTS (
                                  SELECT 1
                                  FROM OPENJSON(task_version.[AuthoringPayloadJson], '$.options') AS correct_option
                                  WHERE JSON_VALUE(correct_option.[value], '$.key') = task_version.[CorrectAnswer])
                          ))
                        THROW 51246, 'Quick Check authoring metadata is invalid.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS task_version
                        INNER JOIN [PresentationElements] AS element
                            ON element.[MaterialVersionId] = task_version.[MaterialVersionId]
                           AND element.[AssessableTaskVersionId] = task_version.[Id]
                        WHERE element.[ElementTypeCode] <> N'QUICK_CHECK'
                           OR task_version.[TaskTypeCode] <> N'SINGLE_CHOICE_QUICK_CHECK')
                        THROW 51247, 'Quick Check element and task version types do not match.', 1;
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

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted element
                        LEFT JOIN [AssessableTaskVersions] task_version
                            ON task_version.[MaterialVersionId] = element.[MaterialVersionId]
                           AND task_version.[Id] = element.[AssessableTaskVersionId]
                        WHERE element.[ElementTypeCode] = N'QUICK_CHECK'
                          AND
                          (
                              task_version.[Id] IS NULL
                              OR task_version.[TaskTypeCode] <> N'SINGLE_CHOICE_QUICK_CHECK'
                          )
                    )
                    BEGIN
                        THROW 51247, 'Quick Check element requires a same-version single-choice task version.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_AssessableTaskVersions_ProtectSnapshot]
                ON [AssessableTaskVersions]
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS task_version
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                       OR EXISTS (
                        SELECT 1
                        FROM deleted AS task_version
                        INNER JOIN [MaterialVersions] AS material_version
                            ON material_version.[Id] = task_version.[MaterialVersionId]
                        WHERE material_version.[Status] <> 1)
                        THROW 51231, 'Assessable task metadata is mutable only within a draft material version.', 1;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted
                        WHERE [TaskTypeCode] = ''
                           OR [TaskTypeCode] COLLATE Latin1_General_100_BIN2
                              LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2
                           OR DATALENGTH([Prompt]) = 0
                           OR ([CorrectAnswer] IS NOT NULL AND DATALENGTH([CorrectAnswer]) = 0)
                           OR ([EvaluationCriterion] IS NOT NULL AND DATALENGTH([EvaluationCriterion]) = 0))
                        THROW 51231, 'Assessable task text and type code must use canonical values.', 1;
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

            migrationBuilder.DropForeignKey(
                name: "FK_PresentationElements_AssessableTaskVersions_MaterialVersionId_AssessableTaskVersionId",
                table: "PresentationElements");

            migrationBuilder.DropIndex(
                name: "IX_PresentationElements_MaterialVersionId_AssessableTaskVersionId",
                table: "PresentationElements");

            migrationBuilder.DropIndex(
                name: "UX_PresentationElements_AssessableTaskVersion",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_AssessableTask",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessableTaskVersions_AuthoringPayload",
                table: "AssessableTaskVersions");

            migrationBuilder.DropColumn(
                name: "AssessableTaskVersionId",
                table: "PresentationElements");

            migrationBuilder.DropColumn(
                name: "AuthoringPayloadJson",
                table: "AssessableTaskVersions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PresentationElements_Type",
                table: "PresentationElements",
                sql: "[ElementTypeCode] IN ('TEXT', 'IMAGE', 'SHAPE', 'TABLE', 'RULE', 'EXAMPLE', 'VOCABULARY', 'QUESTION', 'VIDEO', 'AUDIO')");
        }
    }
}
