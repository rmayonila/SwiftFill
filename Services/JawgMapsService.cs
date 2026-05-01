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

            // Since Jawg Routing works better with coordinates, we'd ideally geocode here.
            // For now, we'll provide a placeholder or use a simple distance estimate if we have coordinates.
            // In a full implementation, we'd geocode origin/dest then call Jawg Routing.
            
            return "ETA: Available on Map";
        }
    }
}
