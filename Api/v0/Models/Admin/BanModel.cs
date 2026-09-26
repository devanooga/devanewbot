namespace devanewbot.Api.v0.Models.Admin;

using System;

public class BanModel
{
    public string? UserId { get; set; }
    public string? ChannelId { get; set; }
    public string? Reason { get; set; }
    public DateOnly? ExpiresOn { get; set; }
}
