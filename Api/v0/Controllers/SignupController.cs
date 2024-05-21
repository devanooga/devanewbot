namespace devanewbot.Api.v0.Controllers;

using System.Threading.Tasks;
using AspNetCore.ReCaptcha;
using devanewbot.Api.v0.Models;
using devanewbot.Services;
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
    protected IReCaptchaService ReCaptchaService { get; }

    protected ReCaptchaSettings ReCaptchaSettings { get; }

    protected InviteService InviteService { get; }


    public SignupController(
         IReCaptchaService reCaptchaService,
         IOptions<ReCaptchaSettings> reCaptchaSettings,
         InviteService inviteService)
    {
        ReCaptchaService = reCaptchaService;
        ReCaptchaSettings = reCaptchaSettings.Value;
        InviteService = inviteService;
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

        await InviteService.CreateInvite(model.Email, Request.HttpContext.Connection.RemoteIpAddress.ToString());
        return Ok();
    }
}
