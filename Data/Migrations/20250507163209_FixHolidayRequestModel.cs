using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rota.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixHolidayRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HolidayRequests_AspNetUsers_UserId",
                table: "HolidayRequests");

            migrationBuilder.DropIndex(
                name: "IX_HolidayRequests_UserId",
                table: "HolidayRequests");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "HolidayRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "HolidayRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_UserId",
                table: "HolidayRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayRequests_AspNetUsers_UserId",
                table: "HolidayRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
