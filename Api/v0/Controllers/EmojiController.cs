namespace devanewbot.Api.v0.Controllers;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SlackNet;

[Route("/api/v0/emoji")]
public class EmojiController : Controller
{
    protected ISlackApiClient Client { get; }

    protected IHttpClientFactory HttpClientFactory { get; }


    public EmojiController(ISlackApiClient client, IHttpClientFactory httpClientFactory)
    {
        Client = client;
        HttpClientFactory = httpClientFactory;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var emojis = await Client.Emoji.List();
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            using var httpClient = HttpClientFactory.CreateClient();
            foreach (var emoji in emojis.Where(e => !e.Value.StartsWith("alias:")))
            {
                var response = await httpClient.GetStreamAsync(emoji.Value);
                var uri = new Uri(emoji.Value);
                var fileExtension = Path.GetExtension(uri.AbsolutePath);
                var entry = archive.CreateEntry($"{emoji.Key}{fileExtension}");
                using var entryStream = entry.Open();
                await response.CopyToAsync(entryStream);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return File(memoryStream.ToArray(), "application/zip", "emoji.zip");
    }
}
