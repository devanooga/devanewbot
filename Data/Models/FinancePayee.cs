namespace devanewbot.Data.Models;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FinancePayee : IEntityTypeConfiguration<FinancePayee>, ICreateable
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<FinancePayee> builder)
    {
        builder.HasIndex(e => e.Name).IsUnique();
    }
}
