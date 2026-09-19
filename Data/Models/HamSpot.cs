namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HamSpot : IEntityTypeConfiguration<HamSpot>, ICreateable
{
    public long Id { get; set; }
    public Guid? SessionId { get; set; }
    public HamSpotSession? Session { get; set; }
    public required string Source { get; set; }
    public required string Callsign { get; set; }
    public required string Reporter { get; set; }
    public string? ReporterGrid { get; set; }
    public string? ReporterCountry { get; set; }
    public string? SpottedGrid { get; set; }
    public long FrequencyHz { get; set; }
    public required string Band { get; set; }
    public required string Mode { get; set; }
    public int? Snr { get; set; }
    public int? Wpm { get; set; }
    public double? DistanceKm { get; set; }
    public string? Comment { get; set; }
    public DateTime HeardAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<HamSpot> builder)
    {
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasOne(e => e.Session).WithMany(e => e.Spots).HasForeignKey(e => e.SessionId).OnDelete(DeleteBehavior.SetNull);
    }
}
