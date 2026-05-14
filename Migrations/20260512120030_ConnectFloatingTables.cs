using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class ConnectFloatingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "1", "a18be9c0-aa65-4af8-bd17-00bd9344e575" });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "2", "b18be9c0-aa65-4af8-bd17-00bd9344e576" });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "3", "c18be9c0-aa65-4af8-bd17-00bd9344e577" });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "4", "d18be9c0-aa65-4af8-bd17-00bd9344e578" });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5", "e18be9c0-aa65-4af8-bd17-00bd9344e579" });

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: "a18be9c0-aa65-4af8-bd17-00bd9344e575");

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: "b18be9c0-aa65-4af8-bd17-00bd9344e576");

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: "c18be9c0-aa65-4af8-bd17-00bd9344e577");

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: "d18be9c0-aa65-4af8-bd17-00bd9344e578");

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: "e18be9c0-aa65-4af8-bd17-00bd9344e579");

            migrationBuilder.AddColumn<int>(
                name: "BrandingSettingsId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemCategoryId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 3, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "07d7b417-dc75-4c7a-8ece-4d30441b94bc");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "3c43979e-e3e9-4d11-b2ec-5f481070e9a4");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "22d9ee75-7e8e-43d2-ab28-75433c411d54");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "36e2194c-99cd-4901-83ca-b8d2dc93c656");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "5",
                column: "ConcurrencyStamp",
                value: "4a50ccb4-f83e-4459-82e0-f6f992f1afd0");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BrandingSettingsId",
                table: "Warehouses",
                column: "BrandingSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ItemCategoryId",
                table: "Orders",
                column: "ItemCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ItemCategories_ItemCategoryId",
                table: "Orders",
                column: "ItemCategoryId",
                principalTable: "ItemCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_BrandingSettings_BrandingSettingsId",
                table: "Warehouses",
                column: "BrandingSettingsId",
                principalTable: "BrandingSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ItemCategories_ItemCategoryId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_BrandingSettings_BrandingSettingsId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BrandingSettingsId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ItemCategoryId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BrandingSettingsId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ItemCategoryId",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "80eb31f6-e542-4978-9b02-ee7008302446");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "35f9bfa6-c8c4-4458-9b82-ed6db236a711");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "e71469d7-490c-4374-a12b-ac0bf19f774b");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "3697195e-1285-4a8a-a7c2-ad0edb9b0752");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "5",
                column: "ConcurrencyStamp",
                value: "5ee6d197-98e3-4b42-94a2-f6b02a94e1e8");

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "Hub", "IsSuspended", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "Route", "SecurityStamp", "TotalFailedLogins", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "a18be9c0-aa65-4af8-bd17-00bd9344e575", 0, "ef20ef9e-2668-4991-8c98-812449c33be3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "superadmin@swiftfill.com", true, "System", null, false, "Administrator", false, null, "SUPERADMIN@SWIFTFILL.COM", "SUPERADMIN", "AQAAAAIAAYagAAAAEHu0Fx+RfU+pW9MTFY66WVV2+X/ociv9804ljeWiURSTgzL0mOTIrD5wZL+XzXfkvw==", "800-555-0199", false, null, "", 0, false, "superadmin" },
                    { "b18be9c0-aa65-4af8-bd17-00bd9344e576", 0, "d06de3ba-5fed-48b1-96e0-52477b496df8", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@swiftfill.com", true, "Warehouse", null, false, "Manager", false, null, "ADMIN@SWIFTFILL.COM", "ADMIN", "AQAAAAIAAYagAAAAEIUU7dEpkJ5mqLtDntn87OS8De8LRQnw4FJlT3Sic2PMfsZIo4XdHEDwjtufvtwb6g==", null, false, null, "", 0, false, "admin" },
                    { "c18be9c0-aa65-4af8-bd17-00bd9344e577", 0, "8ce49dec-f75e-4bc9-85a3-f79ef42db86d", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "staff@swiftfill.com", true, "Warehouse", null, false, "Staff", false, null, "STAFF@SWIFTFILL.COM", "STAFF", "AQAAAAIAAYagAAAAENx6xJxirifsytcWG7557gd5ayO814boiM2rua3khnVfpWS6aL3VBs7OXhdYOPdEdA==", null, false, null, "", 0, false, "staff" },
                    { "d18be9c0-aa65-4af8-bd17-00bd9344e578", 0, "849785fd-bab2-4ba6-b678-822513862992", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "rider@swiftfill.com", true, "Delivery", null, false, "Rider", false, null, "RIDER@SWIFTFILL.COM", "RIDER", "AQAAAAIAAYagAAAAEPlvPbIQ8KGyN7tFqRn52z0GeWvkB05RMo1kf4QgraPqwGxjw8k7w7Hzq6bv3pSDYw==", null, false, null, "", 0, false, "rider" },
                    { "e18be9c0-aa65-4af8-bd17-00bd9344e579", 0, "6da77578-30f1-458c-a4cf-a5b5566776b8", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "customer@swiftfill.com", true, "Alex", null, false, "Doe", false, null, "CUSTOMER@SWIFTFILL.COM", "CUSTOMER", "AQAAAAIAAYagAAAAEKkrewM1bZntq+wysrq+xwzM2EzUU0tumgTxloLemFrj1nwLt7DwWjI0IhIZqWm7FQ==", null, false, null, "", 0, false, "customer" }
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "1", "a18be9c0-aa65-4af8-bd17-00bd9344e575" },
                    { "2", "b18be9c0-aa65-4af8-bd17-00bd9344e576" },
                    { "3", "c18be9c0-aa65-4af8-bd17-00bd9344e577" },
                    { "4", "d18be9c0-aa65-4af8-bd17-00bd9344e578" },
                    { "5", "e18be9c0-aa65-4af8-bd17-00bd9344e579" }
                });
        }
    }
}
