namespace devanewbot.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;
using ImageMagick;
using SlackNet;

public class PropagationService
{
    protected ISlackApiClient Client { get; }

    public PropagationService(ISlackApiClient client)
    {
        Client = client;
    }

    private const string ChannelId = "C3XHU6W9H";

    public async Task PostMessage()
    {
        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var response = await "http://www.hamqsl.com/solar101vhf.php/solar101vhf.php?{timeStamp}".GetAsync();
        // We convert the image to PNG because for some reason Slack scaling gets really funny with gifs (#21)
        using var image = new MagickImage(await response.GetStreamAsync());
        using var memoryStream = new MemoryStream();
        image.Format = MagickFormat.Png64;
        await image.WriteAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        await Client.Files.Upload(memoryStream, "image/png", $"solar101vhf-{timeStamp}.png", channels: new[] { ChannelId });
    }
}
