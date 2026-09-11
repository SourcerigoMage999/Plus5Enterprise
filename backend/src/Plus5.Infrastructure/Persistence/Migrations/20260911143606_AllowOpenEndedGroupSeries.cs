using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plus5.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowOpenEndedGroupSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndsOn",
                table: "RecurringSessionSeries",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [RecurringSessionSeries] WHERE [EndsOn] IS NULL) THROW 51000, 'Cannot downgrade while open-ended series exist. Resolve dates explicitly first.', 1;");
            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndsOn",
                table: "RecurringSessionSeries",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
