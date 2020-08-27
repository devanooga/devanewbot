using devanewbot.Services;
using Microsoft.AspNetCore.Mvc;
using SlackDotNet.Webhooks;
using System.Threading.Tasks;

namespace devanewbot.Controllers
{
    public class CommandController : ControllerBase
    {
        private readonly SpongebobCommand SpongebobCommand;
        private readonly GifCommand GifCommand;

        public CommandController(SpongebobCommand spongebobCommand, GifCommand gifCommand)
        {
            SpongebobCommand = spongebobCommand;
            GifCommand = gifCommand;
        }

        [HttpPost]
        public async Task<OkResult> Spongebob(WebhookMessage webhookMessage)
        {
            await SpongebobCommand.ExecuteAsync(webhookMessage);
            return Ok();
        }

        [HttpPost]
        public async Task<OkResult> Gif(WebhookMessage webhookMessage)
        {
            await GifCommand.ExecuteAsync(webhookMessage);
            return Ok();
        }
    }
}