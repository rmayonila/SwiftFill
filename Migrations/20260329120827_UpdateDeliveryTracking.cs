using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeliveryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempts",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProofImagePath",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAttempts",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProofImagePath",
                table: "Orders");
        }
    }
}
