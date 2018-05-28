using Newtonsoft.Json;

namespace TradingAnalytics.Application.DTO
{
    public class SecurityDTO
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty(PropertyName = "secretKey")]
        public string SecretKey { get; set; }
    }
}
