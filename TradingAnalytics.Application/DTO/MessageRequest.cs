using Newtonsoft.Json;

namespace TradingAnalytics.Application.DTO
{
    public class SendMessageRequestDTO
    {
        [JsonProperty(PropertyName = "chat_id")]
        public int ChatId { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "parse_mode")]
        public string ParseMode { get; set; }

        [JsonProperty(PropertyName = "disable_web_page_preview")]
        public bool DisableWebPagePreview { get; set; }

        [JsonProperty(PropertyName = "disable_notification")]
        public bool DisableNotification { get; set; }
    }
}
