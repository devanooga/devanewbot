using devanewbot.Models.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SlackDotNet;
using SlackDotNet.Payloads;
using System.Threading.Tasks;

namespace devanewbot.Controllers
{
    public class CommandController : ControllerBase
    {
        private Slack _slack { get; set; }
        private IConfiguration _configuration { get; set; }

        public CommandController(Slack slack, IConfiguration configuration)
        {
            _slack = slack;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<OkResult> Spongebob(SpongebobCommand model)
        {
            if (_slack.ValidWebhookMessage(model) && model.UserId != null)
            {
                var slackUser = await _slack.GetUser(model.UserId);
                await _slack.PostMessage(new ChatMessage{
                    Channel = model.ChannelId,
                    Username = slackUser.Profile.DisplayName,
                    Text = model.Response(),
                    IconUrl = slackUser.Profile.ImageOriginal
                });
            }

            return Ok();
        }
    }
}