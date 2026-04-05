using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFulfillmentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Manifests",
                columns: table => new
                {
                    ManifestId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifests", x => x.ManifestId);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    TrackingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    DestinationRegion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ManifestId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssignedRiderId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.TrackingId);
                    table.ForeignKey(
                        name: "FK_Orders_Manifests_ManifestId",
                        column: x => x.ManifestId,
                        principalTable: "Manifests",
                        principalColumn: "ManifestId");
                    table.ForeignKey(
                        name: "FK_Orders_users_AssignedRiderId",
                        column: x => x.AssignedRiderId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    CollectedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_TrackingId",
                        column: x => x.TrackingId,
                        principalTable: "Orders",
                        principalColumn: "TrackingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_users_CollectedByUserId",
                        column: x => x.CollectedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Orders_TrackingId",
                        column: x => x.TrackingId,
                        principalTable: "Orders",
                        principalColumn: "TrackingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedRiderId",
                table: "Orders",
                column: "AssignedRiderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ManifestId",
                table: "Orders",
                column: "ManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CollectedByUserId",
                table: "Payments",
                column: "CollectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TrackingId",
                table: "Payments",
                column: "TrackingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_ReviewedByUserId",
                table: "ReturnRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_TrackingId",
                table: "ReturnRequests",
                column: "TrackingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Manifests");
        }
    }
}
