using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperTrails.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDocumentTypeIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentTypeId",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "DocumentTypeId",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentTypeId",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeId",
                table: "Categories",
                type: "text",
                nullable: true);
        }
    }
}
