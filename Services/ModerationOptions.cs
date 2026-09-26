namespace devanewbot.Services;

public class ModerationOptions
{
    public string? AnnounceChannelId { get; set; }

    public bool AnnounceConfigured => !string.IsNullOrWhiteSpace(AnnounceChannelId) && !AnnounceChannelId.StartsWith("#{");
}
