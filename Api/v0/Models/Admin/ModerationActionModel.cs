namespace devanewbot.Api.v0.Models.Admin;

using System;

public class ModerationActionModel
{
    public string? Action { get; set; }
    public string? Reason { get; set; }
    public DateTime? OccurredAt { get; set; }
}
