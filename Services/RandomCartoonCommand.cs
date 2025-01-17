namespace devanewbot.Services;

using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;

public abstract class RandomCartoonCommand
{
    public async Task SendRandomCartoonImage(
        Url BaseUrl,
        SlashCommand command,
        ISlackApiClient client)
    {
        var randomUrl = new Url(BaseUrl).AppendPathSegment("api/random");
        var randomResponse = await randomUrl.GetJsonAsync<RandomResponse>();
        var imageUrl = new Url(BaseUrl)
            .AppendPathSegment($"img/{randomResponse.Frame.Episode}/{randomResponse.Frame.Timestamp}.jpg");
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

    private class RandomResponse
    {
        public FrameResponse Frame { get; set; } = null!;
    }

    private class FrameResponse
    {
        public long Timestamp { get; set; }
        public string Episode { get; set; } = null!;
    }
}
