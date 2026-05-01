using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftFill.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrandingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoLayout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoHeight = table.Column<int>(type: "int", nullable: false),
                    BrandFontSize = table.Column<double>(type: "float", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandNameColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SidebarColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDarkMode = table.Column<bool>(type: "bit", nullable: false),
                    UseGlassmorphism = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandingSettings");
        }
    }
}
