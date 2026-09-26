namespace devanewbot.Services;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using devanewbot.SlackDotNet.Options;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using SlackNet;

public record SlackIdentity(string UserId, string Name);

public class SlackSignIn(ISlackApiClient client, IOptions<SlackOptions> slackOptions)
{
    public const string LoginProvider = "Slack";

    protected SlackOptions Options { get; } = slackOptions.Value;

    public bool Configured => Options.SignInConfigured;

    public string AuthorizeUrl(string redirectUri, string state) =>
        "https://slack.com/openid/connect/authorize".SetQueryParams(new
        {
            response_type = "code",
            scope = "openid profile",
            client_id = Options.ClientId,
            redirect_uri = redirectUri,
            state,
        });

    /// <returns>null when Slack rejects the code or the account belongs to another workspace.</returns>
    public async Task<SlackIdentity?> Identify(string code, string redirectUri)
    {
        var token = await "https://slack.com/api/openid.connect.token"
            .PostUrlEncodedAsync(new
            {
                client_id = Options.ClientId,
                client_secret = Options.ClientSecret,
                code,
                redirect_uri = redirectUri,
            })
            .ReceiveJson<TokenResponse>();

        if (!token.Ok || string.IsNullOrEmpty(token.AccessToken))
        {
            return null;
        }

        var info = await client.WithAccessToken(token.AccessToken).OpenIdApi.UserInfo(default);
        var workspace = await client.Auth.Test();
        if (info.TeamId != workspace.TeamId || string.IsNullOrEmpty(info.UserId))
        {
            return null;
        }

        return new SlackIdentity(info.UserId, info.Name ?? info.UserId);
    }

    private class TokenResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
