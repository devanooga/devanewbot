namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// Donorbox exports carry emails, addresses and card digits; none of that is kept here.
public class Donation : IEntityTypeConfiguration<Donation>, ICreateable
{
    public Guid Id { get; set; }
    public required string ExternalId { get; set; }
    public DateTime DonatedAt { get; set; }
    public required string Donor { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal Net { get; set; }
    public bool Recurring { get; set; }
    public bool InKind { get; set; }
    public required string Source { get; set; }
    public string? Note { get; set; }
    public bool AnonymousRequested { get; set; }
    public bool NamedOnRequest { get; set; }
    public DateTime? HiddenAt { get; set; }
    public string? HiddenBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.HasIndex(e => e.ExternalId).IsUnique();
        builder.HasIndex(e => e.DonatedAt);
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.Fee).HasPrecision(12, 2);
        builder.Property(e => e.Net).HasPrecision(12, 2);
    }
}
