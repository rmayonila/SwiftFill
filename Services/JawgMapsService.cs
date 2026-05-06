using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SwiftFill.Services
{
    public class JawgMapsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public JawgMapsService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<string> GetDistanceAndTimeAsync(string origin, string destination)
        {
            string token = _configuration["JAWG_ACCESS_TOKEN"] ?? string.Empty;

            if (string.IsNullOrEmpty(token)) 
                return "ETA: API Key Missing";

            try 
            {
                var originCoords = await GeocodeAsync(origin, token);
                var destCoords = await GeocodeAsync(destination, token);

                if (originCoords == null || destCoords == null) 
                    return "ETA: Location not found";

                // Jawg Routing API uses format: {longitude},{latitude};{longitude},{latitude}
                string url = $"https://api.jawg.io/routing/v1/car/{originCoords.Value.Lon},{originCoords.Value.Lat};{destCoords.Value.Lon},{destCoords.Value.Lat}?access-token={token}";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) 
                    return "ETA: Route calculation failed";

                var content = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(content)!;
                
                if (data.routes == null || data.routes.Count == 0)
                    return "ETA: No route found";

                var route = data.routes[0];
                double distanceKm = (double)route.distance / 1000;
                double durationMin = (double)route.duration / 60;

                return $"{distanceKm:F1} km ({System.Math.Round(durationMin)} mins)";
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[JawgMaps] Error: {ex.Message}");
                return "ETA: Service Unavailable";
            }
        }

        private async Task<(double Lat, double Lon)?> GeocodeAsync(string text, string token)
        {
            try 
            {
                // Jawg Places API for geocoding
                string url = $"https://api.jawg.io/places/v1/search?text={System.Uri.EscapeDataString(text)}&access-token={token}&size=1";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(content)!;

                if (data.features == null || data.features.Count == 0) return null;

                // Jawg returns [longitude, latitude]
                var coords = data.features[0].geometry.coordinates;
                return ((double)coords[1], (double)coords[0]);
            }
            catch 
            {
                return null;
            }
        }
    }
}
