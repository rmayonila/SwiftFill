using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class SyncLogisticsMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DeclaredValue",
                table: "Orders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ItemCategory",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeclaredValue",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ItemCategory",
                table: "Orders");
        }
    }
}
