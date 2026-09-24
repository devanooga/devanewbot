namespace devanewbot.Services;

using System;

public class SiteOptions
{
    public string? BaseUrl { get; set; }

    public bool BaseUrlConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !BaseUrl.StartsWith("#{");

    public string? InviteUrl(Guid inviteId) =>
        BaseUrlConfigured ? $"{BaseUrl!.TrimEnd('/')}/admin/invites?id={inviteId}" : null;
}
