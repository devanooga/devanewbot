namespace devanewbot.Api.v0.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SlackDotNet;

[Route("/api/v0/hamalert")]
public class HamAlertController : Controller
{
    protected Slack Slack { get; }

    public HamAlertController(Slack slack)
    {
        Slack = slack;
    }


    [HttpPost("{userId}")]
    public async Task<IActionResult> Alert(string userId, [FromQuery] string fullCallsign, [FromForm] string callSign, [FromForm] string frequency, [FromForm] string band, [FromForm] string mode, [FromForm] string time)
    {
        await Slack.PostMessage(new SlackDotNet.Payloads.ChatMessage
        {
            Channel = userId,
            Text = $"HamAlert: {callSign} {frequency} {band} {mode} {time}"
        });

        return Ok();
    }
}