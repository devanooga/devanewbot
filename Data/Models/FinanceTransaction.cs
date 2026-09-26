namespace devanewbot.Data.Models;

using System;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

[JsonConverter(typeof(JsonStringEnumConverter<FinanceTransactionKind>))]
public enum FinanceTransactionKind
{
    Income,
    Expense,
    Transfer
}

public class FinanceTransaction : IEntityTypeConfiguration<FinanceTransaction>, ICreateable
{
    public Guid Id { get; set; }
    public required string ExternalKey { get; set; }
    public DateOnly Date { get; set; }
    public required string Account { get; set; }
    public FinanceTransactionKind Kind { get; set; }
    public string? Payee { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime? HiddenAt { get; set; }
    public string? HiddenBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<FinanceTransaction> builder)
    {
        builder.HasIndex(e => e.ExternalKey).IsUnique();
        builder.HasIndex(e => e.Date);
        builder.Property(e => e.Amount).HasPrecision(12, 2);
    }
}
