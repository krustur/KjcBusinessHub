using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KjcBusinessHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceVacationWithAbsenceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PublicHolidayDescription",
                table: "OffDays",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPublicHoliday",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "AbsenceType",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
UPDATE OffDays
SET AbsenceType = CASE
    WHEN IsVacation = 1 THEN 1
    ELSE 0
END;
""");

            migrationBuilder.DropColumn(
                name: "IsVacation",
                table: "OffDays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlite())
                throw new NotSupportedException("ReplaceVacationWithAbsenceType rollback requires SQLite-specific validation before converting data to the legacy schema.");

            migrationBuilder.AlterColumn<string>(
                name: "PublicHolidayDescription",
                table: "OffDays",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPublicHoliday",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVacation",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            if (IsSqlite())
            {
                migrationBuilder.Sql("""
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM OffDays WHERE AbsenceType NOT IN (0, 1))
    THEN RAISE(ABORT, 'Absence rows with unsupported legacy values cannot be rolled back to the vacation-only schema automatically.')
END;
""");
            }

            migrationBuilder.Sql("""
UPDATE OffDays
SET IsVacation = CASE
    WHEN AbsenceType = 1 THEN 1
    ELSE 0
END;
""");

            migrationBuilder.DropColumn(
                name: "AbsenceType",
                table: "OffDays");
        }

        private bool IsSqlite() =>
            ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
    }
}
