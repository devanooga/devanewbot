using System.Collections.Generic;
using Newtonsoft.Json;

namespace SlackDotNet.Payloads
{
    public class ChatAttachment
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("callback_id")]
        public string CallbackId { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("actions")]
        public List<ChatAction> Actions { get; set; }
    }
}