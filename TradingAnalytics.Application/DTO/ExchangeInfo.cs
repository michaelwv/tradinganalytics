using System;
using System.Collections.Generic;
using BinanceExchange.API.Converter;
using BinanceExchange.API.Models.Response;
using Newtonsoft.Json;

namespace TradingAnalytics.Application.DTO
{
    public class ExchangeInfoDTO
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "timezone")]
        public string Timezone { get; set; }

        [JsonProperty(PropertyName = "serverTime")]
        [JsonConverter(typeof(EpochTimeConverter))]
        public DateTime ServerTime { get; set; }

        [JsonProperty(PropertyName = "rateLimits")]
        public List<ExchangeInfoRateLimit> RateLimits { get; set; }

        [JsonProperty(PropertyName = "symbols")]
        public List<ExchangeInfoSymbol> Symbols { get; set; }
    }
}
