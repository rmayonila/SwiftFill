using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeLogArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "a18be9c0-aa65-4af8-bd17-00bd9344e575",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "ef20ef9e-2668-4991-8c98-812449c33be3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "AQAAAAIAAYagAAAAEHu0Fx+RfU+pW9MTFY66WVV2+X/ociv9804ljeWiURSTgzL0mOTIrD5wZL+XzXfkvw==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "b18be9c0-aa65-4af8-bd17-00bd9344e576",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "d06de3ba-5fed-48b1-96e0-52477b496df8", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "AQAAAAIAAYagAAAAEIUU7dEpkJ5mqLtDntn87OS8De8LRQnw4FJlT3Sic2PMfsZIo4XdHEDwjtufvtwb6g==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "c18be9c0-aa65-4af8-bd17-00bd9344e577",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "8ce49dec-f75e-4bc9-85a3-f79ef42db86d", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "AQAAAAIAAYagAAAAENx6xJxirifsytcWG7557gd5ayO814boiM2rua3khnVfpWS6aL3VBs7OXhdYOPdEdA==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "d18be9c0-aa65-4af8-bd17-00bd9344e578",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "849785fd-bab2-4ba6-b678-822513862992", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "AQAAAAIAAYagAAAAEPlvPbIQ8KGyN7tFqRn52z0GeWvkB05RMo1kf4QgraPqwGxjw8k7w7Hzq6bv3pSDYw==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "e18be9c0-aa65-4af8-bd17-00bd9344e579",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "6da77578-30f1-458c-a4cf-a5b5566776b8", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "AQAAAAIAAYagAAAAEKkrewM1bZntq+wysrq+xwzM2EzUU0tumgTxloLemFrj1nwLt7DwWjI0IhIZqWm7FQ==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 13, 13, 17, 469, DateTimeKind.Utc).AddTicks(8323));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 13, 13, 17, 470, DateTimeKind.Utc).AddTicks(1221));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 13, 13, 17, 470, DateTimeKind.Utc).AddTicks(1225));

            migrationBuilder.UpdateData(
                table: "ItemCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 4, 13, 13, 17, 470, DateTimeKind.Utc).AddTicks(1227));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "553f6195-aa86-4d92-a131-a66f9d66a41b");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "6af62540-ed7e-4b92-b691-454815d14932");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "3e2b5591-81c8-460f-ae5f-78a7e7b7cac6");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "e75ccb1b-5a31-4895-9990-5fb0a49d59bb");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "5",
                column: "ConcurrencyStamp",
                value: "0bb8c101-2416-4166-ab56-0a4fdb58d1ce");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "a18be9c0-aa65-4af8-bd17-00bd9344e575",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "6608056c-59b5-4e6f-9947-c7ea69fe980a", new DateTime(2026, 5, 4, 13, 13, 17, 473, DateTimeKind.Utc).AddTicks(952), "AQAAAAIAAYagAAAAEIJbCEK3JNhi3HqxpgmwM4o594Tp2aexV5Ma8JbF9bjnysLUt+VO/tToL+8rsxdqnA==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "b18be9c0-aa65-4af8-bd17-00bd9344e576",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "09db9462-4172-47de-aed5-59b3d9eacc04", new DateTime(2026, 5, 4, 13, 13, 17, 645, DateTimeKind.Utc).AddTicks(6584), "AQAAAAIAAYagAAAAEPFnjfFO3YooYmF8i3xyulll2Dy0KuSRBglEsM/Vr7m+5V25X3A0fwjUohsQ1gV0LA==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "c18be9c0-aa65-4af8-bd17-00bd9344e577",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "00560b78-b00e-42c1-b26b-36792d27bf69", new DateTime(2026, 5, 4, 13, 13, 17, 778, DateTimeKind.Utc).AddTicks(4826), "AQAAAAIAAYagAAAAECdFUjhOU872Vo1pug0bdqSjkV8pv1xEWd3Rm3/XKglB9L/k+ZbamEuLmGRiFU1r2A==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "d18be9c0-aa65-4af8-bd17-00bd9344e578",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "577cce80-14ff-4e85-87e4-2f52d56c94a0", new DateTime(2026, 5, 4, 13, 13, 17, 896, DateTimeKind.Utc).AddTicks(8138), "AQAAAAIAAYagAAAAEFOfWgMNob0DogypGxkz4/L5X7cWbDN/v3qSPPAIBTg0g3VwQSQfy2zFFC7qNEf3/w==" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: "e18be9c0-aa65-4af8-bd17-00bd9344e579",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "8242d462-5f28-4a77-b41e-b43b185a8536", new DateTime(2026, 5, 4, 13, 13, 18, 48, DateTimeKind.Utc).AddTicks(1486), "AQAAAAIAAYagAAAAEHTW1ArDuMFpW9QeWiESF5kPY0CZiMbTo70jnzgmXmP73NBJa9TjBeFUO3PiB41fyA==" });
        }
    }
}
