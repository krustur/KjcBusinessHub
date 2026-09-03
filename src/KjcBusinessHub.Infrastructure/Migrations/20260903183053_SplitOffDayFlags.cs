using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KjcBusinessHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitOffDayFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublicHolidayTemp",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVacationTemp",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicHolidayDescriptionTemp",
                table: "OffDays",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM OffDays WHERE OffDayType NOT IN (0, 1, 2))
    THEN RAISE(ABORT, 'Unsupported OffDayType values cannot be migrated automatically.')
END;
""");

            migrationBuilder.Sql("""
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM OffDays WHERE OffDayType = 2)
    THEN RAISE(ABORT, 'Stored BridgingDay rows cannot be migrated automatically. Clear them before applying SplitOffDayFlags.')
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET IsPublicHolidayTemp = CASE
    WHEN OffDayType = 0 THEN 1
    ELSE 0
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET PublicHolidayDescriptionTemp = CASE
    WHEN OffDayType = 0 THEN Description
    ELSE ''
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET IsVacationTemp = CASE
    WHEN OffDayType = 1 THEN 1
    ELSE 0
END;
""");

            migrationBuilder.DropColumn(
                name: "OffDayType",
                table: "OffDays");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "OffDays");

            migrationBuilder.RenameColumn(
                name: "IsPublicHolidayTemp",
                table: "OffDays",
                newName: "IsPublicHoliday");

            migrationBuilder.RenameColumn(
                name: "IsVacationTemp",
                table: "OffDays",
                newName: "IsVacation");

            migrationBuilder.RenameColumn(
                name: "PublicHolidayDescriptionTemp",
                table: "OffDays",
                newName: "PublicHolidayDescription");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM OffDays WHERE IsVacation = 1 AND IsPublicHoliday = 1)
    THEN RAISE(ABORT, 'Combined public-holiday and vacation rows cannot be rolled back to the legacy OffDayType schema automatically.')
END;
""");

            migrationBuilder.Sql("""
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM OffDays WHERE IsVacation = 0 AND IsPublicHoliday = 0)
    THEN RAISE(ABORT, 'Zero-flag OffDay rows cannot be rolled back to the legacy OffDayType schema automatically.')
END;
""");

            migrationBuilder.AddColumn<int>(
                name: "OffDayTypeTemp",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionTemp",
                table: "OffDays",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
UPDATE OffDays
SET OffDayTypeTemp = CASE
    WHEN IsVacation = 1 THEN 1
    WHEN IsPublicHoliday = 1 THEN 0
    ELSE 0
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET DescriptionTemp = CASE
    WHEN IsPublicHoliday = 1 THEN PublicHolidayDescription
    ELSE ''
END;
""");

            migrationBuilder.DropColumn(
                name: "IsPublicHoliday",
                table: "OffDays");

            migrationBuilder.DropColumn(
                name: "PublicHolidayDescription",
                table: "OffDays");

            migrationBuilder.DropColumn(
                name: "IsVacation",
                table: "OffDays");

            migrationBuilder.RenameColumn(
                name: "OffDayTypeTemp",
                table: "OffDays",
                newName: "OffDayType");

            migrationBuilder.RenameColumn(
                name: "DescriptionTemp",
                table: "OffDays",
                newName: "Description");
        }
    }
}
