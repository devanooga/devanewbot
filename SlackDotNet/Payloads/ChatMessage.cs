using Newtonsoft.Json;
using System;

namespace SlackDotNet.Payloads
{

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ChatMessage : Payload
    {
        /// <summary>
        /// Channel, private group, or IM channel to send message to. Can be an encoded ID, or a name.
        /// </summary>
        /// <value></value>
        [JsonProperty("channel")]
        public string Channel { get; set; }

        /// <summary>
        /// The usage of the text field changes depending on whether you're using blocks.
        /// If you are using blocks, this is used as a fallback string to display in notifications.
        /// If you aren't, this is the main body text of the message.
        /// It can be formatted as plain text, or with mrkdwn.
        /// </summary>
        /// <value></value>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Pass true to post the message as the authed user, instead of as a bot. Defaults to false.
        /// </summary>
        /// <value></value>
        [JsonProperty("as_user")]
        public bool? AsUser { get; set; } = null;
        
        /// <summary>
        /// Emoji to use as the icon for this message. Overrides IconUrl.
        /// Must be used in conjunction with AsUser set to `false`, otherwise ignored. 
        /// </summary>
        /// <value></value>
        [JsonProperty("icon_emoji")]
        public string IconEmoji { get; set; }

        /// <summary>
        /// URL to an image to use as the icon for this message.
        /// Must be used in conjunction with AsUser set to `false`, otherwise ignored.
        /// </summary>
        /// <value></value>
        [JsonProperty("icon_url")]
        public Uri IconUrl { get; set; } = null;

        /// <summary>
        /// Find and link channel names and usernames.
        /// </summary>
        /// <value></value>
        [JsonProperty("link_names")]
        public bool? LinkNames { get; set; } = null;

        /// <summary>
        /// Disable Slack markup parsing by setting to `false`. Enabled by default.
        /// </summary>
        /// <value></value>
        [JsonProperty("mrkdwn")]
        public bool? FormatMarkdown { get; set; } = null;

        /// <summary>
        /// Change how messages are treated.
        /// </summary>
        /// <value></value>
        [JsonProperty("parse")]
        public string Parse { get; set; }

        /// <summary>
        /// Used in conjunction with ThreadTimestamp and indicates whether reply should be made visible to everyone in
        /// the channel or conversation.
        /// </summary>
        /// <value></value>
        [JsonProperty("reply_broadcast")]
        public bool? ReplyBroadcast { get; set; } = null;

        /// <summary>
        /// Provide another message's Timestamp value to make this message a reply.
        /// Avoid using a reply's `Timestamp` value; use its parent instead.
        /// </summary>
        /// <value></value>
        [JsonProperty("thread_ts")]
        public string ThreadTimestamp { get; set; }

        /// <summary>
        /// Pass `true` to enable unfurling of primarily text-based content.
        /// </summary>
        /// <value></value>
        [JsonProperty("unfurl_links")]
        public bool? UnfurlLinks { get; set; } = null;

        /// <summary>
        /// Pass `false` to disable unfurling of media content.
        /// </summary>
        /// <value></value>
        [JsonProperty("unfurl_media")]
        public bool? UnfurlMedia { get; set; } = null;

        /// <summary>
        /// Set your bot's user name. Must be used in conjunction with AsUser set to false, otherwise ignored.
        /// </summary>
        /// <value></value>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// Used for Ephemeral messages. User to direct message to.
        /// </summary>
        /// <value></value>
        [JsonProperty("user")]
        public string User { get; set; }
    }
}