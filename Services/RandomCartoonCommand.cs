namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;

public abstract class RandomCartoonCommand
{
    private Random Random = new();

    public async Task SendRandomCartoonImage(
        Url BaseUrl,
        SlashCommand command,
        ISlackApiClient client,
        string? search = null)
    {
        CartoonImageResponse? response = null;
        if (string.IsNullOrEmpty(search))
        {
            var randomUrl = new Url(BaseUrl).AppendPathSegment("api/random");
            response = await randomUrl.GetJsonAsync<CartoonImageResponse>();
        }
        else
        {
            var searchUrl = new Url(BaseUrl).AppendPathSegment("api/search")
                .SetQueryParam("q", search);
            var searchResponse = await searchUrl.GetJsonAsync<FrameResponse[]>();
            response = searchResponse.Length > 0 ? new CartoonImageResponse()
            {
                Frame = searchResponse[Random.Next(searchResponse.Length)]
            } : null;
        }

        if (response is null)
        {
            return;
        }

        var imageUrl = new Url(BaseUrl)
            .AppendPathSegment($"img/{response.Frame.Episode}/{response.Frame.Timestamp}.jpg");
        var slackUser = await client.Users.Info(command.UserId);

        await client.Chat.PostMessage(new Message
        {
            Channel = command.ChannelId,
            Username = slackUser.Profile.DisplayName,
            Text = imageUrl.ToString(),
            Parse = ParseMode.Full,
            UnfurlMedia = true,
            UnfurlLinks = true,
            IconUrl = slackUser.Profile.ImageOriginal
        });
    }

    private class CartoonImageResponse
    {
        public FrameResponse Frame { get; set; } = null!;
    }

    private class FrameResponse
    {
        public long Timestamp { get; set; }
        public string Episode { get; set; } = null!;
    }
}
