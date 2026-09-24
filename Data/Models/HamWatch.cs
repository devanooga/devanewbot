namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HamWatch : IEntityTypeConfiguration<HamWatch>, ICreateable
{
    public Guid Id { get; set; }
    public required string Callsign { get; set; }
    public string? Grid { get; set; }
    public string? SlackUserId { get; set; }
    public required string AddedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<HamWatch> builder)
    {
        builder.HasIndex(e => e.Callsign).IsUnique();
    }
}
