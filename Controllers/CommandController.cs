using Microsoft.AspNetCore.Mvc;
using SlackAPI;
using devanewbot.Models.Commands;

namespace devanewbot.Controllers
{
    public class CommandController : ControllerBase
    {
        private SlackTaskClient _slackClient { get; set; }

        public CommandController(SlackTaskClient slackClient)
        {
            _slackClient = slackClient;
        }

        [HttpPost]
        public OkResult Spongebob(SpongebobCommand model)
        {
            _slackClient.PostMessageAsync(model.Channel_Name, model.Response(), model.User_Name, null, false, null, null, null, null, null, true);

            return Ok();
        }
    }
}