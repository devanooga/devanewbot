namespace devanewbot.Api.v0.Controllers;

using System.Threading.Tasks;
using Devanewbot.Api.v0.Models.HamAlert;
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
    public async Task<IActionResult> Alert(string userId, [FromBody] AlertModel model)
    {
        await Client.Chat.PostMessage(new Message
        {
            Channel = userId,
            Text = $"HamAlert: {model.CallSign} {model.Frequency} {model.Band} {model.Mode} {model.Time}"
        });

        return Ok();
    }
}
