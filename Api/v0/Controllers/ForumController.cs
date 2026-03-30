namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.WebApi;

[Route("/api/v0/forums")]
public class ForumController : Controller
{
    protected ILogger<ForumController> Logger { get; }

    protected ISlackApiClient SlackApiClient { get; }

    private readonly IMemoryCache _cache;

    private static readonly TimeSpan ReplyCooldown = TimeSpan.FromMinutes(30);

    public ForumController(
        ILogger<ForumController> logger,
        ISlackApiClient slackApiClient,
        IMemoryCache cache)
    {
        Logger = logger;
        SlackApiClient = slackApiClient;
        _cache = cache;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> WebHook([FromBody] JsonElement body)
    {
        var contentType = body.GetProperty("content_type").GetString();

        if (contentType == "post")
        {
            return await HandlePostWebhook(body);
        }

        return await HandleThreadWebhook(body);
    }

    private async Task<IActionResult> HandleThreadWebhook(JsonElement body)
    {
        var model = body.Deserialize<WebHookModel>();
        var channel = await ResolveChannel(model.Data.Forum.Title);
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

    private async Task<IActionResult> HandlePostWebhook(JsonElement body)
    {
        var model = body.Deserialize<PostWebHookModel>();
        var threadId = model.Data.Thread?.ThreadId ?? model.Data.ThreadId;
        var cacheKey = $"forum-reply-{threadId}";

        if (_cache.TryGetValue(cacheKey, out _))
        {
            Logger.LogDebug("Skipping reply cross-post for thread {ThreadId} (cooldown active)", threadId);
            return Ok();
        }

        var forumTitle = model.Data.Thread?.Forum?.Title;
        if (forumTitle == null)
        {
            Logger.LogWarning("Post webhook for thread {ThreadId} missing forum info, skipping", threadId);
            return Ok();
        }

        var channel = await ResolveChannel(forumTitle);
        var threadTitle = model.Data.Thread.Title;
        var postUrl = model.Data.ViewUrl ?? model.Data.Thread.ViewUrl;

        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = channel,
            Text = $"{model.Data.Username} replied to {Link.Url(postUrl, threadTitle)}",
            Username = $"Forums - {model.Data.Username}",
            IconUrl = model.Data.User?.AvatarUrls?.H,
            UnfurlLinks = true,
            UnfurlMedia = true,
        });

        _cache.Set(cacheKey, true, ReplyCooldown);
        return Ok();
    }

    private async Task<string> ResolveChannel(string forumTitle)
    {
        var conversations = await SlackApiClient.Conversations.List(true, 1000, [ConversationType.PublicChannel, ConversationType.PrivateChannel]);
        return conversations.Channels.FirstOrDefault(c => c.Name.Equals(forumTitle, StringComparison.InvariantCultureIgnoreCase))?.Id
            ?? forumTitle switch
            {
                "Meta" => "C01DPUF43NZ",
                "Tech News" => "C01EGSCSYQY",
                "True Beginners" => "C405VESSU",
                "C#" => "C3XKVAJ4S",
                "Dungeons and Dragons" => "CEWMTRQ14",
                "Home Improvement" => "C9VDL8CMD",
                "General" => "C01EGSCSYQY",
                _ => throw new Exception($"{forumTitle} not mapped to a Slack channel")
            };
    }
}
