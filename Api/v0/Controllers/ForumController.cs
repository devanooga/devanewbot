namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.WebApi;

[Route("/api/v0/forums")]
public class ForumController : Controller
{
    protected ILogger<ForumController> Logger { get; }

    protected ISlackApiClient SlackApiClient { get; }

    public ForumController(
        ILogger<ForumController> logger,
        ISlackApiClient slackApiClient)
    {
        Logger = logger;
        SlackApiClient = slackApiClient;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> WebHook([FromBody] WebHookModel model)
    {
        var conversations = await SlackApiClient.Conversations.List(true, 1000, [ConversationType.PublicChannel, ConversationType.PrivateChannel]);
        var channel = conversations.Channels.FirstOrDefault(c => c.Name.Equals(model.Data.Forum.Title, StringComparison.InvariantCultureIgnoreCase))?.Id
            ?? model.Data.Forum.Title switch
            {
                // Support for custom non-matching name matches
                "Meta" => "C01DPUF43NZ",
                "Tech News" => "C01EGSCSYQY",
                "True Beginners" => "C405VESSU",
                "C#" => "C3XKVAJ4S",
                "Dungeons and Dragons" => "CEWMTRQ14",
                "Home Improvement" => "C9VDL8CMD",
                "General" => "C01EGSCSYQY", // General is a renamed channel? It's making the lookup fail, just map it.
                _ => throw new Exception($"{model.Data.Forum.Title} not mapped to a Slack channel")
            };
        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = channel,
            Text = $"{Link.Url(model.Data.ViewUrl, model.Data.Title)}",
            Username = $"Forums - {model.Data.Username}",
            IconUrl = model.Data.User.AvatarUrls.H,
            UnfurlLinks = true,
            UnfurlMedia = true,
        });
        return Ok();
    }
}
