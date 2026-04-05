using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentLocationToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentLocation",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "Orders");
        }
    }
}
