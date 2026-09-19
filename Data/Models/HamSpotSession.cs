namespace devanewbot.Data.Models;

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HamSpotSession : IEntityTypeConfiguration<HamSpotSession>, ICreateable
{
    public Guid Id { get; set; }
    public required string Callsign { get; set; }
    public required string Band { get; set; }
    public required string Mode { get; set; }
    public string? Grid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastHeardAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? RenderedAt { get; set; }
    public string? SlackChannelId { get; set; }
    public string? SlackMessageTs { get; set; }
    public int SpotCount { get; set; }
    public int ReporterCount { get; set; }
    public double? FurthestKm { get; set; }
    public string? FurthestReporter { get; set; }
    public int? BestSnr { get; set; }
    public string? BestSnrReporter { get; set; }
    public ICollection<HamSpot> Spots { get; set; } = [];

    public void Configure(EntityTypeBuilder<HamSpotSession> builder)
    {
        builder.HasIndex(e => new { e.Callsign, e.Band, e.Mode, e.ClosedAt });
        builder.HasIndex(e => e.CreatedAt);
    }
}
