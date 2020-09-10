using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SlackDotNet.Payloads;

namespace SlackDotNet.Webhooks
{
    public class InteractiveMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("actions")]
        public List<ChatAction> Actions { get; set; }

        [JsonProperty("callback_id")]
        public string CallbackId { get; set; }

        [JsonProperty("team")]
        public InteractiveAttribute Team { get; set; }

        [JsonProperty("channel")]
        public InteractiveAttribute Channel { get; set; }

        [JsonProperty("user")]
        public InteractiveAttribute User { get; set; }

        [JsonProperty("action_ts")]
        public string ActionTs { get; set; }

        [JsonProperty("message_ts")]
        public string MessageTs { get; set; }

        [JsonProperty("attachment_id")]
        public string AttachmentId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("is_app_unfurl")]
        public bool IsAppUnfurl { get; set; }

        [JsonProperty("response_url")]
        public Uri ResponseUrl { get; set; }

        [JsonProperty("trigger_id")]
        public string TriggerId { get; set; }
    }
}