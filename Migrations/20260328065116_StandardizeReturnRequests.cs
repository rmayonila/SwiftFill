using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeReturnRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ReturnRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ReturnRequests");
        }
    }
}
