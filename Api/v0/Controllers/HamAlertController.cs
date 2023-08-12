namespace devanewbot.Api.v0.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SlackNet;
using SlackNet.WebApi;

[Route("/api/v0/hamalert")]
public class HamAlertController : Controller
{
    protected ISlackApiClient Client { get; }

    public HamAlertController(ISlackApiClient client)
    {
        Client = client;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> Alert(string userId, [FromQuery] string fullCallsign, [FromForm] string callSign, [FromForm] string frequency, [FromForm] string band, [FromForm] string mode, [FromForm] string time)
    {
        await Client.Chat.PostMessage(new Message
        {
            Channel = userId,
            Text = $"HamAlert: {callSign} {frequency} {band} {mode} {time}"
        });

        return Ok();
    }
}