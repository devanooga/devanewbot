namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HamCallsignLocation : IEntityTypeConfiguration<HamCallsignLocation>
{
    public required string Callsign { get; set; }
    public string? Grid { get; set; }
    public required string Source { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void Configure(EntityTypeBuilder<HamCallsignLocation> builder)
    {
        builder.HasKey(e => e.Callsign);
    }
}
