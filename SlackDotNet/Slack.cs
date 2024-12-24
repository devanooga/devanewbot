namespace SlackDotNet;

using System.Net.Http;
using System.Threading.Tasks;
using devanewbot.SlackDotNet.Options;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

public class Slack(IOptions<SlackOptions> options)
{
    private SlackOptions Options { get; set; } = options.Value;

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

    public async Task<(bool Success, string Error)> DisableUser(string userId)
    {
        var response = await $"https://devanooga.slack.com/api/users.admin.setInactive"
            .SendJsonAsync(HttpMethod.Delete, new
            {
                token = Options.LegacyToken,
                user = userId
            });

        var body = await response.GetStringAsync();
        var json = JObject.Parse(body);
        if (!json["ok"].Value<bool>())
        {
            return (false, json["error"].ToString());
        }

        return (true, null);
    }
}
