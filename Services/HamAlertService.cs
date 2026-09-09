namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class HamAlertService
{
    private const string LoginFailureMarker = "Login failed; please check username and password";

    private readonly HttpClient client;
    private readonly SemaphoreSlim listLock = new(1, 1);
    private readonly string username;
    private readonly string password;
    private bool loggedIn;

    public HamAlertService(IConfiguration configuration)
    {
        var section = configuration.GetSection("HamAlert");
        username = section.GetValue<string>("Username")!;
        password = section.GetValue<string>("Password")!;
        ListName = section.GetValue<string>("TriggerComment")!;

        client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() })
        {
            BaseAddress = new Uri("https://hamalert.org")
        };
    }

    public string ListName { get; }

    public async Task<string[]> ListCallsigns()
    {
        await listLock.WaitAsync();
        try
        {
            return ReadCallsigns(await FetchTrigger());
        }
        finally
        {
            listLock.Release();
        }
    }

    public async Task<string[]> AddCallsigns(IEnumerable<string> callsigns)
    {
        await listLock.WaitAsync();
        try
        {
            var trigger = await FetchTrigger();
            var current = ReadCallsigns(trigger);
            var added = callsigns.Except(current, StringComparer.OrdinalIgnoreCase).Distinct().ToArray();

            if (added.Length > 0)
            {
                await SaveCallsigns(trigger, [.. current, .. added]);
            }

            return added;
        }
        finally
        {
            listLock.Release();
        }
    }

    public async Task<string[]> RemoveCallsigns(IEnumerable<string> callsigns)
    {
        await listLock.WaitAsync();
        try
        {
            var trigger = await FetchTrigger();
            var current = ReadCallsigns(trigger);
            var removed = current.Intersect(callsigns, StringComparer.OrdinalIgnoreCase).ToArray();

            if (removed.Length > 0)
            {
                await SaveCallsigns(trigger, [.. current.Except(removed, StringComparer.OrdinalIgnoreCase)]);
            }

            return removed;
        }
        finally
        {
            listLock.Release();
        }
    }

    private async Task<JsonNode> FetchTrigger()
    {
        var response = await Send(() => new HttpRequestMessage(HttpMethod.Get, "/ajax/triggers"));
        var triggers = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();

        return triggers.FirstOrDefault(trigger =>
                string.Equals(trigger?["comment"]?.GetValue<string>(), ListName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No HamAlert trigger named \"{ListName}\" was found on the shared account.");
    }

    private static string[] ReadCallsigns(JsonNode trigger) =>
        trigger["conditions"]?["callsign"] switch
        {
            JsonArray list => [.. list.Select(callsign => callsign!.GetValue<string>()).Order(StringComparer.Ordinal)],
            JsonValue single => [single.GetValue<string>()],
            _ => []
        };

    private async Task SaveCallsigns(JsonNode trigger, string[] callsigns)
    {
        if (callsigns.Length == 0)
        {
            throw new InvalidOperationException(
                $"HamAlert rejects an empty callsign list, so \"{ListName}\" has to keep at least one callsign.");
        }

        var updated = trigger.DeepClone();
        updated["conditions"]!["callsign"] = new JsonArray([.. callsigns.Order(StringComparer.Ordinal).Select(callsign => JsonValue.Create(callsign))]);

        await Send(() => new HttpRequestMessage(HttpMethod.Post, "/ajax/trigger_update")
        {
            Content = JsonContent.Create(updated)
        });
    }

    private async Task<HttpResponseMessage> Send(Func<HttpRequestMessage> request)
    {
        if (!loggedIn)
        {
            await Login();
        }

        var response = await client.SendAsync(request());

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await Login();
            response = await client.SendAsync(request());
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException($"HamAlert rejected the change: {await response.Content.ReadAsStringAsync()}");
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task Login()
    {
        loggedIn = false;

        var response = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password
        }));
        response.EnsureSuccessStatusCode();

        if ((await response.Content.ReadAsStringAsync()).Contains(LoginFailureMarker))
        {
            throw new InvalidOperationException("HamAlert login failed; check the shared account credentials.");
        }

        loggedIn = true;
    }
}
