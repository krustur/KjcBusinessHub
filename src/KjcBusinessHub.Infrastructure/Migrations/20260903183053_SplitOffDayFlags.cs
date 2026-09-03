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
            migrationBuilder.RenameColumn(
                name: "OffDayType",
                table: "OffDays",
                newName: "IsVacation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "OffDays",
                newName: "PublicHolidayDescription");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublicHoliday",
                table: "OffDays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM OffDays WHERE IsVacation = 2)
    THEN RAISE(ABORT, 'Stored BridgingDay rows cannot be migrated automatically. Clear them before applying SplitOffDayFlags.')
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET IsPublicHoliday = CASE
    WHEN IsVacation = 0 THEN 1
    ELSE 0
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET PublicHolidayDescription = CASE
    WHEN IsVacation = 0 THEN PublicHolidayDescription
    ELSE ''
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET IsVacation = CASE
    WHEN IsVacation = 1 THEN 1
    ELSE 0
END;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
UPDATE OffDays
SET PublicHolidayDescription = CASE
    WHEN IsPublicHoliday = 1 THEN PublicHolidayDescription
    ELSE ''
END;
""");

            migrationBuilder.Sql("""
UPDATE OffDays
SET IsVacation = CASE
    WHEN IsVacation = 1 THEN 1
    WHEN IsPublicHoliday = 1 THEN 0
    ELSE 2
END;
""");

            migrationBuilder.DropColumn(
                name: "IsPublicHoliday",
                table: "OffDays");

            migrationBuilder.RenameColumn(
                name: "PublicHolidayDescription",
                table: "OffDays",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "IsVacation",
                table: "OffDays",
                newName: "OffDayType");
        }
    }
}
