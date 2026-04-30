using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class AddManualRiderToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManualRiderId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ManualRiderId",
                table: "Orders",
                column: "ManualRiderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ManualRiders_ManualRiderId",
                table: "Orders",
                column: "ManualRiderId",
                principalTable: "ManualRiders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ManualRiders_ManualRiderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ManualRiderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ManualRiderId",
                table: "Orders");
        }
    }
}
