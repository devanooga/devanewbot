namespace SlackDotNet;

using System;
using System.IO;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SlackDotNet.Options;
using SlackDotNet.Payloads;
using SlackDotNet.Responses;
using SlackDotNet.Webhooks;

public class Slack
{
    private SlackOptions Options { get; set; }

    public Slack(IOptions<SlackOptions> options)
    {
        Options = options.Value;
    }

    /// <summary>
    /// Verifies the authenticity of an interactive message from Slack.
    /// Should be used before acting on an interactive message.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool ValidInteractiveMessage(InteractiveMessage model)
    {
        return model.Token == Options.VerificationToken;
    }

    /// <summary>
    /// Get's a slack user's information
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<User> GetUser(string userId)
    {
        var response = await "https://slack.com/api/users.info"
            .SetQueryParam("token", Options.OauthToken)
            .SetQueryParam("user", userId)
            .GetJsonAsync<Response>();

        return response.User;
    }

    /// <summary>
    /// Posts a message to a channel
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> PostMessage(ChatMessage message, bool ephemeral = false)
    {
        var endpoint = ephemeral ? "postEphemeral" : "postMessage";
        var response = await $"https://slack.com/api/chat.{endpoint}"
            .WithHeader("Authorization", "Bearer " + Options.OauthToken)
            .PostJsonAsync(message);
        if (response.StatusCode != 200)
        {
            throw new Exception($"Error posting message to Slack: {await response.GetStringAsync()}");
        }

        return true;
    }

    /// <summary>
    /// Deletes a message in response to an interactive command
    /// </summary>
    /// <param name="responseUrl"></param>
    /// <returns></returns>
    public async Task<bool> DeleteResponse(string responseUrl)
    {
        var response = await responseUrl
            .WithHeader("Authorization", "Bearer " + Options.OauthToken)
            .PostJsonAsync(new
            {
                delete_original = true
            });

        return true;
    }

    /// <summary>
    /// Deletes a message in response to an interactive command
    /// </summary>
    /// <param name="responseUrl"></param>
    /// <returns></returns>
    public async Task<(bool Success, string Error)> InviteUser(string email)
    {
        var response = await $"https://devanooga.slack.com/api/users.admin.invite"
            .PostUrlEncodedAsync(new
            {
                token = Options.LegacyToken,
                email,
                set_active = true
            });

        var body = await response.GetStringAsync();
        var json = JObject.Parse(body);
        if (!json["ok"].Value<bool>())
        {
            return (false, json["error"].ToString());
        }

        return (true, null);
    }

    public async Task<bool> UploadFile(Stream stream, string fileName, string contentType, string[] channelIds = null)
    {
        var response = await "https://slack.com/api/files.upload"
            .WithHeader("Authorization", "Bearer " + Options.OauthToken)
            .PostMultipartAsync(builder =>
            {
                builder
                    .AddFile("file", stream, fileName, contentType);
                if (channelIds is not null)
                {
                    builder.AddString("channels", string.Join(",", channelIds));
                }
            });
        var body = await response.GetStringAsync();
        var json = JObject.Parse(body);
        if (!json["ok"].Value<bool>())
        {
            throw new Exception(body);
        }

        return response.ResponseMessage.IsSuccessStatusCode;
    }
}
