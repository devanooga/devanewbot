namespace devanewbot.Services.Ham;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class HamFeedStatus
{
    public record Feed(string Name, bool Connected, DateTime? ConnectedAt, DateTime? LastSpotAt, string? LastError);

    private readonly ConcurrentDictionary<string, Feed> feeds = new();

    public void Connected(string name) =>
        feeds.AddOrUpdate(name,
            _ => new Feed(name, true, DateTime.UtcNow, null, null),
            (_, feed) => feed with { Connected = true, ConnectedAt = DateTime.UtcNow, LastError = null });

    public void Disconnected(string name, string? error) =>
        feeds.AddOrUpdate(name,
            _ => new Feed(name, false, null, null, error),
            (_, feed) => feed with { Connected = false, LastError = error });

    public void SpotReceived(string name) =>
        feeds.AddOrUpdate(name,
            _ => new Feed(name, true, DateTime.UtcNow, DateTime.UtcNow, null),
            (_, feed) => feed with { LastSpotAt = DateTime.UtcNow });

    public IReadOnlyList<Feed> Snapshot() => [.. feeds.Values.OrderBy(feed => feed.Name)];
}
