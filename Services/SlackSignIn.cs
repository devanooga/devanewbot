namespace devanewbot.Services;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using devanewbot.SlackDotNet.Options;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackNet;

public record SlackIdentity(string UserId, string Name);

public class SlackSignIn(
    ILogger<SlackSignIn> logger,
    ISlackApiClient client,
    IMemoryCache cache,
    IOptions<SlackOptions> slackOptions)
{
    public const string LoginProvider = "Slack";

    protected SlackOptions Options { get; } = slackOptions.Value;

    public bool Configured => Options.SignInConfigured;

    public async Task<string> AuthorizeUrl(string redirectUri, string state) =>
        "https://slack.com/openid/connect/authorize".SetQueryParams(new
        {
            response_type = "code",
            scope = "openid profile",
            client_id = Options.ClientId,
            redirect_uri = redirectUri,
            state,
            team = await WorkspaceTeamId(),
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
            logger.LogWarning("Slack refused the sign-in code exchange: {Error}", token.Error);
            return null;
        }

        var info = await client.WithAccessToken(token.AccessToken).OpenIdApi.UserInfo(default);
        var workspaceTeamId = await WorkspaceTeamId();
        if (info.TeamId != workspaceTeamId || string.IsNullOrEmpty(info.UserId))
        {
            logger.LogWarning(
                "Slack sign-in for user {UserId} came from team {SignInTeamId} ({SignInTeamName}), but the bot is in team {BotTeamId}",
                info.UserId,
                info.TeamId,
                info.TeamName,
                workspaceTeamId);
            return null;
        }

        return new SlackIdentity(info.UserId, info.Name ?? info.UserId);
    }

    private async Task<string> WorkspaceTeamId() =>
        (await cache.GetOrCreateAsync("slack-workspace-team-id", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return (await client.Auth.Test()).TeamId;
        }))!;

    private class TokenResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
