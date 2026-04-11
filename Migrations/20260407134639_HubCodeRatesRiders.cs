using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class HubCodeRatesRiders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupBranchAddress",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "HubAccessCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HubName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HubAccessCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManualRiders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Hub = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualRiders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestinationRegion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ZoneSurcharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HubAccessCodes_HubName_IsActive",
                table: "HubAccessCodes",
                columns: new[] { "HubName", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HubAccessCodes");

            migrationBuilder.DropTable(
                name: "ManualRiders");

            migrationBuilder.DropTable(
                name: "ShippingRates");

            migrationBuilder.DropColumn(
                name: "PickupBranchAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "Orders");
        }
    }
}
