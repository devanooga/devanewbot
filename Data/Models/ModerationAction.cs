namespace devanewbot.Data.Models;

using System;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

[JsonConverter(typeof(JsonStringEnumConverter<ModerationActionKind>))]
public enum ModerationActionKind
{
    RemovedMessage,
    Deactivated,
    ChannelBan,
    ChannelBanLifted,
    Other
}

[JsonConverter(typeof(JsonStringEnumConverter<ModerationActionSource>))]
public enum ModerationActionSource
{
    Slack,
    Admin,
    Imported
}

public class ModerationAction : IEntityTypeConfiguration<ModerationAction>, ICreateable
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public ModerationActionKind Kind { get; set; }
    public ModerationActionSource Source { get; set; }
    public required string Action { get; set; }
    public required string Reason { get; set; }
    public required string Administrator { get; set; }
    public string? AdministratorSlackUserId { get; set; }
    public string? TargetSlackUserId { get; set; }
    public string? ChannelId { get; set; }
    public string? RemovedMessageText { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.TargetSlackUserId);
    }
}
