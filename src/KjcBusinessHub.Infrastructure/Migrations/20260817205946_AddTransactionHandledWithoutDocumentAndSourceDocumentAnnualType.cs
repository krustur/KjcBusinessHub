using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KjcBusinessHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionHandledWithoutDocumentAndSourceDocumentAnnualType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHandledWithoutDocument",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AnnualType",
                table: "SourceDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHandledWithoutDocument",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AnnualType",
                table: "SourceDocuments");
        }
    }
}
