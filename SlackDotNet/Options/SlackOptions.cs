namespace devanewbot.SlackDotNet.Options;

public class SlackOptions
{
    public string OauthToken { get; set; } = null!;
    public string UserToken { get; set; } = null!;
    public string SigningSecret { get; set; } = null!;
    public string VerificationToken { get; set; } = null!;
    public string LegacyToken { get; set; } = null!;
    public string GeneralChannelId { get; set; } = null!;
}
