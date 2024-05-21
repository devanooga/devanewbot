namespace devanewbot.Api.v0.Controllers;

using System.Threading.Tasks;
using AspNetCore.ReCaptcha;
using devanewbot.Api.v0.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackDotNet;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

[Route("/api/v0/signup")]
public class SignupController : Controller
{
    protected Slack Slack { get; }

    protected IReCaptchaService ReCaptchaService { get; }

    protected ReCaptchaSettings ReCaptchaSettings { get; }

    protected ILogger<SignupController> Logger { get; }

    protected ISlackApiClient SlackApiClient { get; }

    public SignupController(Slack slack,
        ISlackApiClient slackApiClient,
         IReCaptchaService reCaptchaService,
         IOptions<ReCaptchaSettings> reCaptchaSettings,
         ILogger<SignupController> logger)
    {
        Slack = slack;
        SlackApiClient = slackApiClient;
        ReCaptchaService = reCaptchaService;
        ReCaptchaSettings = reCaptchaSettings.Value;
        Logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetToken()
    {
        return Task.FromResult<IActionResult>(Ok(new
        {
            Token = ReCaptchaSettings.SiteKey
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Signup([FromBody] SignupModel model)
    {
        if (!await ReCaptchaService.VerifyAsync(model.Token))
        {
            return BadRequest(new { Error = "token_verification_failed" });
        }

        Logger.LogInformation("Sending invite to {email} from IP {ip}", model.Email, Request.HttpContext.Connection.RemoteIpAddress);

        var channel = "C074VF1PC7K";
        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = channel,
            Text = $"Sending invite to {model.Email} from IP {Request.HttpContext.Connection.RemoteIpAddress}",
            Blocks = new Block[] {
                new ActionsBlock
                {
                    Elements =
                    {
                        new SlackNet.Blocks.Button
                        {

                            ActionId = "approve",
                            Text = "Approve Account",
                            Value = model.Email,
                            Style = ButtonStyle.Default
                        },
                        new SlackNet.Blocks.Button
                        {

                            ActionId = "disable",
                            Text = "Disable Account",
                            Value = model.Email,
                            Style = ButtonStyle.Danger
                        },
                    }
                }
            }
        });
        return Ok();
    }
    public async Task Handle(ButtonAction action, BlockActionRequest request)
    {
        Logger.LogInformation("Action: {}", action.ActionId);
        var commandingUser = await SlackApiClient.Users.Info(request.User.Id);
        switch (action.ActionId)
        {
            case "disable":
                var slackUser = await SlackApiClient.Users.LookupByEmail(action.Value);
                await SlackApiClient.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), null);
                await Slack.DisableUser(slackUser.Id);
                await SlackApiClient.Chat.PostMessage(new Message
                {
                    Channel = request.Channel.Id,
                    Parse = ParseMode.Full,
                    Text = $"{commandingUser.Profile.DisplayName} disabled {action.Value}",
                    UnfurlLinks = true,
                });
                break;
            case "approve":
                var (success, error) = await Slack.InviteUser(action.Value);
                await SlackApiClient.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), null);
                if (!success)
                {
                    await SlackApiClient.Chat.PostMessage(new Message
                    {
                        Channel = request.Channel.Id,
                        Parse = ParseMode.Full,
                        Text = $"{commandingUser.Profile.DisplayName} approved {action.Value} but we had an error: {error}",
                        UnfurlLinks = true,
                    });
                }
                else
                {
                    await SlackApiClient.Chat.PostMessage(new Message
                    {
                        Channel = request.Channel.Id,
                        Parse = ParseMode.Full,
                        Text = $"{commandingUser.Profile.DisplayName} approved {action.Value}",
                        UnfurlLinks = true,
                    });
                }

                break;
        }

    }
}
