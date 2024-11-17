namespace devanewbot.Entities;

using System;

public class ChannelBan
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string ChannelId { get; set; }
    public required string Reason { get; set; }
    public required string BannedBy { get; set; }
    public DateTime BannedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool LiftedEarly { get; set; }
    public bool Active { get; set; }
}