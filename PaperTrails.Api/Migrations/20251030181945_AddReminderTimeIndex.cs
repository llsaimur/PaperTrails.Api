using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperTrails.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderTimeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ReminderTime",
                table: "Reminders",
                column: "ReminderTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
