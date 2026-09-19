namespace devanewbot.Services.Ham;

using System;

public enum HamSessionState
{
    Live,
    Quiet,
    Closed
}

public static class HamSessionLifecycle
{
    public static HamSessionState Of(DateTime lastHeardAt, DateTime? closedAt, int staleAfterMinutes) =>
        closedAt is not null ? HamSessionState.Closed
        : DateTime.UtcNow - lastHeardAt >= TimeSpan.FromMinutes(staleAfterMinutes) ? HamSessionState.Quiet
        : HamSessionState.Live;
}
