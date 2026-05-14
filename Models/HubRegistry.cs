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

        /// <summary>
        /// Matches a destination region/island group to the correct final destination hub.
        /// </summary>
        public static string ResolveDestinationHub(string destinationRegion, string fullAddress = "")
        {
            // 1. Try matching against the full address first (most specific)
            if (!string.IsNullOrEmpty(fullAddress))
            {
                var matchedHub = All.FirstOrDefault(h => 
                    fullAddress.Contains(h.Name.Replace(" Hub", ""), StringComparison.OrdinalIgnoreCase) ||
                    fullAddress.Contains(h.Region, StringComparison.OrdinalIgnoreCase));
                
                if (matchedHub != null) return matchedHub.Name;

                // City-specific keywords
                if (fullAddress.Contains("Davao", StringComparison.OrdinalIgnoreCase)) return "Davao Hub";
                if (fullAddress.Contains("Manila", StringComparison.OrdinalIgnoreCase) || 
                    fullAddress.Contains("NCR", StringComparison.OrdinalIgnoreCase) ||
                    fullAddress.Contains("Makati", StringComparison.OrdinalIgnoreCase) ||
                    fullAddress.Contains("Quezon City", StringComparison.OrdinalIgnoreCase)) return "Manila Hub";
                if (fullAddress.Contains("Cebu", StringComparison.OrdinalIgnoreCase)) return "Cebu Hub";
                if (fullAddress.Contains("Iloilo", StringComparison.OrdinalIgnoreCase)) return "Iloilo Hub";
                if (fullAddress.Contains("Bacolod", StringComparison.OrdinalIgnoreCase)) return "Bacolod Hub";
                if (fullAddress.Contains("Zamboanga", StringComparison.OrdinalIgnoreCase)) return "Zamboanga Hub";
                if (fullAddress.Contains("General Santos", StringComparison.OrdinalIgnoreCase) || 
                    fullAddress.Contains("Gensan", StringComparison.OrdinalIgnoreCase)) return "General Santos Hub";
                if (fullAddress.Contains("Cagayan de Oro", StringComparison.OrdinalIgnoreCase) || 
                    fullAddress.Contains("CDO", StringComparison.OrdinalIgnoreCase)) return "Cagayan de Oro Hub";
            }

            // 2. Direct region/name matches from the region field
            var hub = All.FirstOrDefault(h =>
                h.Region.Equals(destinationRegion, StringComparison.OrdinalIgnoreCase) ||
                h.Name.Contains(destinationRegion, StringComparison.OrdinalIgnoreCase));
            if (hub != null) return hub.Name;

            // 3. Island-group fallback
            return destinationRegion switch
            {
                "NCR"      => "Manila Hub",
                "Luzon"    => "Manila Hub",
                "Visayas"  => "Cebu Hub",
                "Mindanao" => "Cagayan de Oro Hub",
                _          => $"{destinationRegion} Hub"
            };
        }
    }

    public record HubInfo(string Name, string Region, string Island);
}
