using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class ConnectFloatingTables_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "d7a3a532-7e36-48bf-966d-e7f93a971019");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "a24e7a92-13d1-49e9-869f-83e849396329");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "2bcb82e3-ba64-4ff9-a20d-331829df8e2f");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "6e3707bc-e210-415b-afdf-37fc56c05f68");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "5",
                column: "ConcurrencyStamp",
                value: "8a0660ff-2048-45db-ac8c-c34337d1c475");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
