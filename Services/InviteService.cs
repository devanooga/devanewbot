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
using System.Text.Json.Serialization;
using global::SlackDotNet;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Invite = devanewbot.Data.Models.Invite;

public class InviteService(
    Slack slack,
    ISlackApiClient slackApiClient,
    DevanewbotContext db,
    ILogger<InviteService> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<SiteOptions> siteOptions) : IBlockActionHandler<ButtonAction>
{
    protected Slack Slack { get; } = slack;
    protected ISlackApiClient SlackApiClient { get; } = slackApiClient;
    protected DevanewbotContext Db { get; } = db;
    protected ILogger<InviteService> Logger { get; } = logger;
    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    protected SiteOptions Site { get; } = siteOptions.Value;
    private const string GeoIpApi = "http://ip-api.com/json/";
    private const string GeoIpFields = "status,message,country,regionName,city,lat,lon,timezone,isp,org,as,reverse,mobile,proxy,hosting";

    private const string Channel = "C074VF1PC7K";

    public async Task<InviteResult> CreateInvite(string email, string ip)
    {
        email = email.Trim();
        var normalized = email.ToLowerInvariant();
        if (await Db.Invites.AnyAsync(invite =>
                invite.Status == InviteStatus.Pending && invite.Email.ToLower() == normalized))
        {
            Logger.LogInformation("Ignoring repeat signup for {email} from {ip}, an invite is already pending", email, ip);
            return InviteResult.AlreadyPending;
        }

        var locationInfo = await GetLocationInfo(ip);
        var (flag, message) = EvaluateLocation(locationInfo);

        var invite = new Invite
        {
            Email = email,
            Ip = ip,
            Source = InviteSource.Signup,
            Status = InviteStatus.Pending,
            Flag = flag.ToString(),
            FlagMessage = message,
            City = locationInfo?.City,
            Region = locationInfo?.Region,
            Country = locationInfo?.Country,
            Isp = locationInfo?.Isp,
            Proxy = locationInfo?.Proxy ?? false,
            Hosting = locationInfo?.Hosting ?? false,
            Mobile = locationInfo?.Mobile ?? false,
            LocationJson = locationInfo is null ? null : JsonSerializer.Serialize(locationInfo)
        };
        Db.Invites.Add(invite);
        await Db.SaveChangesAsync();

        if (flag == LocationFlag.Green)
        {
            try
            {
                return await Decide(invite, approve: true, "Automatic", InviteDecisionSource.Automatic);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to automatically approve invite for {email} from {ip}", email, ip);
                invite.Status = InviteStatus.Pending;
            }
        }

        await PostInviteMessage(invite, BuildRequestMessage(email, ip, locationInfo, flag, message));
        return InviteResult.Queued;
    }

    public async Task<InviteResult> CreateAdminInvite(string email, string adminEmail)
    {
        email = email.Trim();
        var normalized = email.ToLowerInvariant();
        var pending = await Db.Invites.FirstOrDefaultAsync(invite =>
            invite.Status == InviteStatus.Pending && invite.Email.ToLower() == normalized);
        if (pending is not null)
        {
            return await Decide(pending, approve: true, adminEmail, InviteDecisionSource.Admin);
        }

        var invite = new Invite
        {
            Email = email,
            Ip = "admin panel",
            Source = InviteSource.Admin,
            Status = InviteStatus.Pending
        };
        Db.Invites.Add(invite);
        await Db.SaveChangesAsync();

        return await Decide(invite, approve: true, adminEmail, InviteDecisionSource.Admin);
    }

    public async Task<InviteResult> DecideFromAdmin(Guid inviteId, bool approve, string adminEmail)
    {
        var invite = await Db.Invites.FindAsync(inviteId)
            ?? throw new InvalidOperationException("That invite does not exist.");

        return await Decide(invite, approve, adminEmail, InviteDecisionSource.Admin);
    }

    private async Task PostInviteMessage(Invite invite, string message)
    {
        var response = await SlackApiClient.Chat.PostMessage(new Message
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
                            Value = invite.Id.ToString(),
                            Style = ButtonStyle.Primary
                        },
                        new SlackNet.Blocks.Button
                        {
                            ActionId = "decline_invite",
                            Text = "Decline Invite",
                            Value = invite.Id.ToString(),
                            Style = ButtonStyle.Danger
                        }
                    }
                }
            }
        });

        invite.SlackChannelId = response.Channel;
        invite.SlackMessageTs = response.Ts;
        await Db.SaveChangesAsync();
    }

    public async Task Handle(ButtonAction action, BlockActionRequest request)
    {
        var commandingUser = await SlackApiClient.Users.Info(request.User.Id);
        var invite = await FindInvite(action.Value);

        if (invite is null)
        {
            Logger.LogError("Failed to resolve invite from button payload");
            await SlackApiClient.Chat.PostMessage(new Message
            {
                Channel = request.Channel.Id,
                Parse = ParseMode.Full,
                Text = $"Failed to deserialize signup payload! Could not handle request.",
            });
            return;
        }

        try
        {
            await Decide(
                invite,
                approve: action.ActionId == "approve_invite",
                DecidedByName(commandingUser),
                InviteDecisionSource.Slack,
                request,
                request.User.Id);
        }
        catch (InvalidOperationException e)
        {
            await SlackApiClient.Chat.PostMessage(new Message
            {
                Channel = request.Channel.Id,
                Text = e.Message
            });
        }
    }

    // Slack leaves the display name empty for anyone who never set one, and the invite log has nothing
    // else to show for a decision made from Slack.
    private static string DecidedByName(SlackNet.User user) =>
        new[] { user.Profile?.DisplayName, user.Profile?.RealName, user.RealName, user.Name }
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? user.Id;

    private async Task<Invite?> FindInvite(string payload)
    {
        if (Guid.TryParse(payload, out var id))
        {
            return await Db.Invites.FindAsync(id);
        }

        SignupPayload? legacy;
        try
        {
            legacy = JsonSerializer.Deserialize<SignupPayload>(payload);
        }
        catch (JsonException)
        {
            return null;
        }

        if (legacy is null)
        {
            return null;
        }

        var invite = new Invite
        {
            Email = legacy.Email,
            Ip = legacy.Ip,
            Source = InviteSource.Signup,
            Status = InviteStatus.Pending,
            City = legacy.LocationInfo?.City,
            Region = legacy.LocationInfo?.Region,
            Country = legacy.LocationInfo?.Country,
            Isp = legacy.LocationInfo?.Isp,
            Proxy = legacy.LocationInfo?.Proxy ?? false,
            Hosting = legacy.LocationInfo?.Hosting ?? false,
            Mobile = legacy.LocationInfo?.Mobile ?? false,
            LocationJson = legacy.LocationInfo is null ? null : JsonSerializer.Serialize(legacy.LocationInfo)
        };
        Db.Invites.Add(invite);
        await Db.SaveChangesAsync();
        return invite;
    }

    private async Task<InviteResult> Decide(
        Invite invite,
        bool approve,
        string decidedBy,
        InviteDecisionSource source,
        BlockActionRequest? request = null,
        string? decidedBySlackUserId = null)
    {
        if (invite.Status != InviteStatus.Pending)
        {
            throw new InvalidOperationException($"{invite.Email} was already {Describe(invite.Status)} by {invite.DecidedBy} on {invite.DecidedAt:yyyy-MM-dd HH:mm} UTC.");
        }

        var location = invite.City is null ? invite.Ip : $"{invite.City}, {invite.Region}, {invite.Country}";
        invite.DecidedAt = DateTime.UtcNow;
        invite.DecidedBy = decidedBy;
        invite.DecidedBySlackUserId = decidedBySlackUserId;
        invite.DecisionSource = source;

        string text;
        var result = InviteResult.Approved;
        if (approve)
        {
            var (success, error) = await Slack.InviteUser(invite.Email);
            if (success)
            {
                invite.Status = InviteStatus.Approved;
                text = $"Invite approved for {invite.Email} from {location} by {decidedBy}";
            }
            else if (error == "already_in_team" || error == "already_in_team_invited_user")
            {
                invite.Status = InviteStatus.AlreadyInvited;
                result = InviteResult.AlreadyInvited;
                text = $"Re-signup attempt for {invite.Email} from {location} (already invited, no action taken)";
            }
            else
            {
                invite.Status = InviteStatus.Failed;
                invite.Error = error;
                await Db.SaveChangesAsync();
                await SlackApiClient.Chat.PostMessage(new Message
                {
                    Channel = Channel,
                    Parse = ParseMode.Full,
                    Text = $"{decidedBy} approved {invite.Email} from {invite.Ip} but we had an error: {error}",
                    UnfurlLinks = true,
                });
                throw new Exception($"Failed to invite user: {error}");
            }
        }
        else
        {
            invite.Status = InviteStatus.Declined;
            text = $"Invite declined for {invite.Email} from {location} by {decidedBy}";
        }

        await Db.SaveChangesAsync();

        await RemoveRequestMessage(invite, request);

        if (Site.InviteUrl(invite.Id) is { } detailsUrl)
        {
            text += $" (<{detailsUrl}|details>)";
        }

        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = Channel,
            Parse = ParseMode.Full,
            Text = text
        });
        return result;
    }

    // The response_url only lives 30 minutes and these requests routinely sit longer than that, so the
    // stored timestamp is the only delete that survives a slow decision.
    private async Task RemoveRequestMessage(Invite invite, BlockActionRequest? request)
    {
        if (invite.SlackMessageTs is not null && invite.SlackChannelId is not null)
        {
            try
            {
                await SlackApiClient.Chat.Delete(invite.SlackMessageTs, invite.SlackChannelId);
                return;
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Could not delete the Slack request message for invite {InviteId}", invite.Id);
            }
        }

        if (request is not null)
        {
            await SlackApiClient.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), CancellationToken.None);
        }
    }

    private static string Describe(InviteStatus status) => status switch
    {
        InviteStatus.AlreadyInvited => "found to be already invited",
        InviteStatus.Failed => "attempted and failed",
        _ => status.ToString().ToLowerInvariant()
    };

    private static string BuildRequestMessage(string email, string ip, LocationInfo? locationInfo, LocationFlag flag, string message)
    {
        var lines = new List<string> { $"Invite request for {email} from IP {ip}" };

        if (locationInfo is null)
        {
            lines.Add("Error: Failed to determine location");
        }
        else
        {
            lines.Add($"Location: {locationInfo.City}, {locationInfo.Region}, {locationInfo.Country} ({locationInfo.Timezone})");
            lines.Add($"ISP: {locationInfo.Isp} | ASN: {locationInfo.AutonomousSystem}");
            if (!string.IsNullOrEmpty(locationInfo.Org) && locationInfo.Org != locationInfo.Isp)
            {
                lines.Add($"Org: {locationInfo.Org}");
            }
            if (!string.IsNullOrEmpty(locationInfo.Hostname))
            {
                lines.Add($"Hostname: {locationInfo.Hostname}");
            }
            var signals = new List<string>();
            if (locationInfo.Proxy) signals.Add("VPN/Proxy/Tor");
            if (locationInfo.Hosting) signals.Add("Datacenter");
            if (locationInfo.Mobile) signals.Add("Mobile");
            if (signals.Count > 0)
            {
                lines.Add($"Type: {string.Join(", ", signals)}");
            }
        }

        lines.Add($"{FlagAsEmoji(flag)} {message}");
        lines.Add(string.Join(" | ",
            $"<https://whatismyipaddress.com/ip/{ip}|whatismyipaddress>",
            $"<https://www.abuseipdb.com/check/{ip}|AbuseIPDB>",
            $"<https://www.google.com/search?q={Uri.EscapeDataString($"\"{email}\"")}|Google email>"));

        return string.Join("\n", lines);
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
            var response = await client.GetAsync($"{GeoIpApi}{ip}?fields={GeoIpFields}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var locationInfo = JsonSerializer.Deserialize<LocationInfo>(content);
            if (locationInfo?.Status != "success")
            {
                Logger.LogError("GeoIP lookup failed for IP {ip}: {message}", ip, locationInfo?.Message);
                return null;
            }
            return locationInfo;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to get location info for IP {ip}", ip);
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

        if (locationInfo.Proxy)
        {
            return (LocationFlag.Yellow, "VPN/proxy detected. Please check email results");
        }

        if (locationInfo.Hosting)
        {
            return (LocationFlag.Yellow, "Datacenter IP. Please check email results");
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
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("city")]
    public string City { get; set; } = null!;
    [JsonPropertyName("regionName")]
    public string Region { get; set; } = null!;
    [JsonPropertyName("country")]
    public string Country { get; set; } = null!;
    [JsonPropertyName("lat")]
    public double Lat { get; set; }
    [JsonPropertyName("lon")]
    public double Lon { get; set; }
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
    [JsonPropertyName("isp")]
    public string? Isp { get; set; }
    [JsonPropertyName("org")]
    public string? Org { get; set; }
    [JsonPropertyName("as")]
    public string? AutonomousSystem { get; set; }
    [JsonPropertyName("reverse")]
    public string? Hostname { get; set; }
    [JsonPropertyName("mobile")]
    public bool Mobile { get; set; }
    [JsonPropertyName("proxy")]
    public bool Proxy { get; set; }
    [JsonPropertyName("hosting")]
    public bool Hosting { get; set; }
}

public enum LocationFlag
{
    Green,
    Yellow,
    Red
}

public enum InviteResult
{
    Approved,
    Queued,
    AlreadyInvited,
    AlreadyPending
}
