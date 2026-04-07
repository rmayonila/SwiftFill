namespace SwiftFill.Models
{
    /// <summary>
    /// Central registry of all SwiftFill hub locations across the Philippines.
    /// This is the single source of truth for hub → region → island mapping.
    /// Any hub can originate or receive a parcel — there is no fixed "origin" hub.
    /// </summary>
    public static class HubRegistry
    {
        public static readonly List<HubInfo> All = new()
        {
            new HubInfo("Davao Hub",           "Davao Region",           "Mindanao"),
            new HubInfo("Manila Hub",           "National Capital Region","Luzon"),
            new HubInfo("Cebu Hub",             "Central Visayas",        "Visayas"),
            new HubInfo("Cagayan de Oro Hub",   "Northern Mindanao",      "Mindanao"),
            new HubInfo("Iloilo Hub",           "Western Visayas",        "Visayas"),
            new HubInfo("Bacolod Hub",          "Negros Occidental",      "Visayas"),
            new HubInfo("Zamboanga Hub",        "Zamboanga Peninsula",    "Mindanao"),
            new HubInfo("General Santos Hub",   "Region XII",             "Mindanao"),
        };

        /// <summary>Returns all hub names as a simple list.</summary>
        public static List<string> Names => All.Select(h => h.Name).ToList();

        /// <summary>Returns the island group for a given hub name.</summary>
        public static string? GetIsland(string hubName) =>
            All.FirstOrDefault(h => h.Name == hubName)?.Island;

        /// <summary>Returns the region for a given hub name.</summary>
        public static string? GetRegion(string hubName) =>
            All.FirstOrDefault(h => h.Name == hubName)?.Region;
    }

    public record HubInfo(string Name, string Region, string Island);
}
