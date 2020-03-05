using System.Threading.Tasks;
using devanewbot.Models;
using devanewbot.Models.Commands;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SlackAPI;

namespace devanewbot.Controllers
{
    public class CommandController : ControllerBase
    {
        private SlackTaskClient _slackClient { get; set; }
        private IConfiguration _configuration { get; set; }

        public CommandController(SlackTaskClient slackClient, IConfiguration configuration)
        {
            _slackClient = slackClient;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<OkResult> Spongebob(SpongebobCommand model)
        {
            if (model.UserId != null && model.Token == _configuration["Secrets:SlackVerificationToken"])
            {
                var slackUser = await "https://slack.com/api/users.info"
                    .WithHeader("Authorization", "Bearer " + _configuration["Secrets:SlackOauthToken"])
                    .SetQueryParam("token", _configuration["Secrets:SlackOauthToken"])
                    .SetQueryParam("user", model.UserId) 
                    .GetJsonAsync<SlackUser>();
                await "https://slack.com/api/chat.postMessage"
                    .WithHeader("Authorization", "Bearer " + _configuration["Secrets:SlackOauthToken"])
                    .PostJsonAsync(new {
                        channel = model.ChannelId,
                        username = slackUser.User.Profile.DisplayName,
                        text = model.Response(),
                        icon_url = slackUser.User.Profile.ImageOriginal
                    });
            }

            return Ok();
        }
    }
}