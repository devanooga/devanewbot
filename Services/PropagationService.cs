namespace devanewbot.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;
using ImageMagick;
using SlackDotNet;

public class PropagationService
{
    public readonly Slack Slack;

    public PropagationService(Slack slack)
    {
        Slack = slack;
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
        await Slack.UploadFile(memoryStream, $"solar101vhf-{timeStamp}.png", "image/png", new[] { ChannelId });
    }
}
