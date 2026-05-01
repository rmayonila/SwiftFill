using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Models
{
    public class BrandingSettings
    {
        [Key]
        public int Id { get; set; }

        // Logo
        public string? LogoUrl { get; set; }
        public string? TintedLogoUrl { get; set; }
        public string? BrandName { get; set; }
        public string? LogoLayout { get; set; } // row, column, logo-only
        public int LogoHeight { get; set; } = 60;
        public double BrandFontSize { get; set; } = 1.4;

        // Colors
        public string PrimaryColor { get; set; } = "#ff8c00";
        public string BackgroundColor { get; set; } = "#161b22";
        public string BrandNameColor { get; set; } = "#ffffff";
        public string SidebarColor { get; set; } = "#0a0a0a";

        // Theme
        public bool IsDarkMode { get; set; } = true;
        public bool UseGlassmorphism { get; set; } = true;
    }
}
