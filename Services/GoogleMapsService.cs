using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SwiftFill.Services
{
    public class GoogleMapsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GoogleMapsService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<string> GetDistanceAndTimeAsync(string origin, string destination)
        {
            string apiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GOOGLE_MAPS_API_KEY_HERE" || apiKey == "SET_IN_ENV_FILE") 
                return "ETA: API Key Missing";

            string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origin}&destinations={destination}&key={apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic? data = JsonConvert.DeserializeObject(json);
                    
                    if (data != null && data.status == "OK")
                    {
                        var element = data.rows[0].elements[0];
                        if (element.status == "OK")
                        {
                            string distance = element.distance.text;
                            string duration = element.duration.text;
                            return $"ETA: {duration} ({distance})";
                        }
                    }
                }
            }
            catch
            {
                return "ETA: Error calculating";
            }

            return "ETA: Not available";
        }
    }
}
