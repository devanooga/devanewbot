namespace devanewbot.Api.v0.Models.Admin;

using System.Collections.Generic;
using devanewbot.Services;

public class RemovalModel
{
    public string? UserId { get; set; }
    public List<MessageToRemove> Messages { get; set; } = [];
    public string? Reason { get; set; }
    public bool Deactivate { get; set; }
}
