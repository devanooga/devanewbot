namespace devanewbot.Services;

using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;
using System;
using SlackDotNet;
using System.Text.Json.Serialization;

public class InviteService : IBlockActionHandler<ButtonAction>
{
    protected Slack Slack { get; }
    protected ISlackApiClient SlackApiClient { get; }
    protected ILogger<InviteService> Logger { get; }
    protected IHttpClientFactory HttpClientFactory { get; }
    protected Uri GeoIpApi = new("http://ip-api.com/json/");

    private const string Channel = "C074VF1PC7K";

    public InviteService(
        Slack slack,
        ISlackApiClient slackApiClient,
        ILogger<InviteService> logger,
        IHttpClientFactory httpClientFactory)
    {
        Slack = slack;
        SlackApiClient = slackApiClient;
        Logger = logger;
        HttpClientFactory = httpClientFactory;
    }

    public async Task CreateInvite(string email, string ip)
    {
        var locationInfo = await GetLocationInfo(ip);
        var (flag, message) = EvaluateLocation(locationInfo);

        var payload = JsonSerializer.Serialize(new SignupPayload
        {
            Email = email,
            Ip = ip,
            LocationInfo = locationInfo
        });

        var markdownText = locationInfo is null
            ? $"Invite request for {email} from IP {ip}\nError: Failed to determine location"
            : $"Invite request for {email} from IP {ip}\nLocation: {locationInfo.City}, {locationInfo.Region}, {locationInfo.Country}\n{FlagAsEmoji(flag)} {message}";

        if (flag == LocationFlag.Green)
        {
            try
            {
                await HandleInvite(approve: true, email, ip, locationInfo);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to automatically approve invite for {email} from {ip}", email, ip);
                await PostInviteMessage(markdownText, payload);
            }
            return;
        }

        await PostInviteMessage(markdownText, payload);
    }

    public async Task PostInviteMessage(string message, string payload)
    {
        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = Channel,
            Blocks = new Block[]
            {
                new SectionBlock
                {
                    Text = new Markdown(message)
                },
                new ActionsBlock
                {
                    Elements = new IActionElement[]
                    {
                        new SlackNet.Blocks.Button
                        {
                            ActionId = "approve_invite",
                            Text = "Approve Invite",
                            Value = payload,
                            Style = ButtonStyle.Primary
                        },
                        new SlackNet.Blocks.Button
                        {
                            ActionId = "decline_invite",
                            Text = "Decline Invite",
                            Value = payload,
                            Style = ButtonStyle.Danger
                        }
                    }
                }
            }
        });
    }

    public async Task Handle(ButtonAction action, BlockActionRequest request)
    {
        var commandingUser = await SlackApiClient.Users.Info(request.User.Id);
        var signup = JsonSerializer.Deserialize<SignupPayload>(action.Value);

        if (signup is null)
        {
            Logger.LogError("Failed to deserialize signup payload");
            await SlackApiClient.Chat.PostMessage(new Message
            {
                Channel = request.Channel.Id,
                Parse = ParseMode.Full,
                Text = $"Failed to deserialize signup payload! Could not handle request.",
            });
            return;
        }

        switch (action.ActionId)
        {
            case "decline_invite":
                await HandleInvite(
                    approve: false,
                    signup.Email,
                    signup.Ip,
                    signup.LocationInfo,
                    request,
                    commandingUser.Profile.DisplayName);
                break;
            case "approve_invite":
                await HandleInvite(
                    approve: true,
                    signup.Email,
                    signup.Ip,
                    signup.LocationInfo,
                    request,
                    commandingUser.Profile.DisplayName);
                break;
        }
    }

    private async Task HandleInvite(
        bool approve,
        string email,
        string ip,
        LocationInfo? locationInfo,
        BlockActionRequest? request = null,
        string approver = "Automatic")
    {
        string text;
        if (approve)
        {
            var (success, error) = await Slack.InviteUser(email);
            if (!success)
            {
                await SlackApiClient.Chat.PostMessage(new Message
                {
                    Channel = Channel,
                    Parse = ParseMode.Full,
                    Text = $"{approver} approved {email} from {ip} but we had an error: {error}",
                    UnfurlLinks = true,
                });
                throw new Exception($"Failed to invite user: {error}");
            }

            text = locationInfo is null
                ? $"Invite approved for {email} from {ip} by {approver}"
                : $"Invite approved for {email} from {locationInfo.City}, {locationInfo.Region}, {locationInfo.Country} by {approver}";
        }
        else
        {
            text = locationInfo is null
                ? $"Invite declined for {email} from {ip} by {approver}"
                : $"Invite declined for {email} from {locationInfo.City}, {locationInfo.Region}, {locationInfo.Country} by {approver}";
        }

        if (request is not null)
        {
            await SlackApiClient.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), null);
        }
        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = Channel,
            Parse = ParseMode.Full,
            Text = text
        });
    }

    private static string FlagAsEmoji(LocationFlag flag) => flag switch
    {
        LocationFlag.Green => ":large_green_square:",
        LocationFlag.Yellow => ":large_yellow_square:",
        LocationFlag.Red => ":large_red_square:",
        _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, null)
    };

    private async Task<LocationInfo?> GetLocationInfo(string ip)
    {
        try
        {
            using var client = HttpClientFactory.CreateClient();
            var response = await client.GetAsync(new Uri(GeoIpApi, ip));
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LocationInfo>(content);
        }
        catch (Exception)
        {
            Logger.LogError("Failed to get location info for IP {ip}", ip);
            return null;
        }
    }

    private (LocationFlag LocationFlag, string Message) EvaluateLocation(LocationInfo? locationInfo)
    {
        if (locationInfo is null)
        {
            return (LocationFlag.Yellow, "Failed to determine location. Please check email results");
        }

        if (locationInfo.Country != "United States")
        {
            return (LocationFlag.Red, "Overseas");
        }

        if ((locationInfo.Region == "Tennessee" && locationInfo.City == "Chattanooga")
            || IsNearbyChattanooga(locationInfo.Lat, locationInfo.Lon))
        {
            return (LocationFlag.Green, "Local");
        }

        return (LocationFlag.Yellow, "Please check email results");
    }

    private static bool IsNearbyChattanooga(double lat, double lon)
    {
        const double chattanoogaLat = 35.0456;
        const double chattanoogaLon = -85.3097;
        const double maxDistance = 50; // miles

        var distance = CalculateDistance(lat, lon, chattanoogaLat, chattanoogaLon);
        return distance <= maxDistance;
    }

    // Calculate distance in miles with the Haversine Formula
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 3960; // Distance in Miles
        var lat = ToRadians(lat2 - lat1);
        var lng = ToRadians(lon2 - lon1);
        var h1 = Math.Sin(lat / 2) * Math.Sin(lat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(lng / 2) * Math.Sin(lng / 2);
        var h2 = 2 * Math.Asin(Math.Min(1, Math.Sqrt(h1)));
        return R * h2;
    }

    private static double ToRadians(double val) => Math.PI / 180 * val;
}

public class SignupPayload
{
    public string Email { get; set; } = null!;
    public string Ip { get; set; } = null!;
    public LocationInfo? LocationInfo { get; set; }
}

public class LocationInfo
{
    [JsonPropertyName("city")]
    public string City { get; set; } = null!;
    [JsonPropertyName("region")]
    public string Region { get; set; } = null!;
    [JsonPropertyName("country")]
    public string Country { get; set; } = null!;
    [JsonPropertyName("lat")]
    public double Lat { get; set; }
    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}

public enum LocationFlag
{
    Green,
    Yellow,
    Red
}
