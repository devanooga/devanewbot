namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FinanceAccount : IEntityTypeConfiguration<FinanceAccount>, ICreateable
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateOnly OpeningDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<FinanceAccount> builder)
    {
        builder.HasIndex(e => e.Name).IsUnique();
        builder.Property(e => e.OpeningBalance).HasPrecision(12, 2);
    }
}
