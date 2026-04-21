using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderForPackingAndTrucking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hub",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AvailPacking",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PackedByStaffId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackingFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PackingLocation",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SortingStatus",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckLabel",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hub",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AvailPacking",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PackedByStaffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PackingFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PackingLocation",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SortingStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TruckLabel",
                table: "Orders");
        }
    }
}
