using Microsoft.AspNetCore.Mvc;

namespace devanewbot.Models.Commands
{
    public class SlashCommand
    {
        public string Token { get; set; }
        public string Text { get; set; }
        public string Command { get; set; }

        [FromForm(Name = "teamId")]
        public string TeamId { get; set; }

        [FromForm(Name = "team_domain")]
        public string TeamDomain { get; set; }

        [FromForm(Name = "channel_id")]
        public string ChannelId { get; set; }

        [FromForm(Name = "channel_name")]
        public string ChannelName { get; set; }

        [FromForm(Name = "user_id")]
        public string UserId { get; set; }

        [FromForm(Name = "user_name")]
        public string UserName { get; set; }

        [FromForm(Name = "response_url")]
        public string ResponseUrl { get; set; }

        [FromForm(Name = "trigger_id")]
        public string TriggerId { get; set; }
    }
}