namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public enum InviteStatus
{
    Pending,
    Approved,
    Declined,
    AlreadyInvited,
    Failed
}

public enum InviteSource
{
    Signup,
    Admin
}

public enum InviteDecisionSource
{
    Automatic,
    Slack,
    Admin
}

public class Invite : IEntityTypeConfiguration<Invite>, ICreateable
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Ip { get; set; }
    public InviteSource Source { get; set; }
    public InviteStatus Status { get; set; }
    public string? Flag { get; set; }
    public string? FlagMessage { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? Isp { get; set; }
    public bool Proxy { get; set; }
    public bool Hosting { get; set; }
    public bool Mobile { get; set; }
    public string? LocationJson { get; set; }
    public string? SlackChannelId { get; set; }
    public string? SlackMessageTs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? DecidedBySlackUserId { get; set; }
    public InviteDecisionSource? DecisionSource { get; set; }
    public string? Error { get; set; }

    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.Property(e => e.Source).HasConversion<string>();
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.DecisionSource).HasConversion<string>();
        builder.Property(e => e.LocationJson).HasColumnType("jsonb");
        builder.HasIndex(e => e.Email);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.Status);
    }
}
