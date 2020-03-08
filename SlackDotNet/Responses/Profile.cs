using Newtonsoft.Json;
using System;

namespace SlackDotNet.Responses
{
    public class Profile
    {
        [JsonProperty("avatar_hash")]
        public string AvatarHash { get; set; }

        [JsonProperty("status_text")]
        public string StatusText { get; set; }

        [JsonProperty("status_emoji")]
        public string StatusEmoji { get; set; }

        [JsonProperty("real_name")]
        public string RealName { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("real_name_normalized")]
        public string RealNameNormalized { get; set; }

        [JsonProperty("display_name_normalized")]
        public string DisplayNameNormalized { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("image_original")]
        public Uri ImageOriginal { get; set; }

        [JsonProperty("image_24")]
        public Uri Image24 { get; set; }

        [JsonProperty("image_32")]
        public Uri Image32 { get; set; }

        [JsonProperty("image_48")]
        public Uri Image48 { get; set; }

        [JsonProperty("image_72")]
        public Uri Image72 { get; set; }

        [JsonProperty("image_192")]
        public Uri Image192 { get; set; }

        [JsonProperty("image_512")]
        public Uri Image512 { get; set; }

        [JsonProperty("team")]
        public string Team { get; set; }
    }
}