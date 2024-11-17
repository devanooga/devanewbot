namespace SlackDotNet.Options;

public class SlackOptions
{
    public string OauthToken { get; set; }
    public string UserToken { get; set; }
    public string SigningSecret { get; set; }
    public string VerificationToken { get; set; }
    public string LegacyToken { get; set; }
}
