namespace devanewbot.Services.Finance;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// NamedOnRequest is null for Donorbox rows, which leave that choice to the admin panel.
public record DonationRow(
    string ExternalId,
    DateTime DonatedAt,
    string Donor,
    decimal Amount,
    decimal Fee,
    decimal Net,
    bool Recurring,
    bool InKind,
    string Source,
    bool AnonymousRequested,
    bool? NamedOnRequest,
    string? Note);

// Reads either a Donorbox export or the direct-donations file kept for gifts that never went through Donorbox.
public static class DonationCsv
{
    public const string DirectHeader = "Id,Date,Donor,Amount,Type,Public,Note";

    public static List<DonationRow> Parse(string csv)
    {
        var lines = Csv.Records(csv).Where(cells => cells.Any(cell => cell.Trim().Length > 0)).ToList();
        if (lines.Count == 0)
        {
            throw new FormatException("That file is empty.");
        }

        var header = lines[0].Select(cell => cell.Trim().TrimStart('﻿')).ToArray();
        return header.Contains("Receipt Id", StringComparer.OrdinalIgnoreCase)
            ? ParseDonorbox(header, lines.Skip(1))
            : ParseDirect(header, lines.Skip(1));
    }

    public static string WriteDirect(IEnumerable<DonationRow> rows)
    {
        var output = new StringBuilder(DirectHeader).Append('\n');
        foreach (var row in rows)
        {
            output.AppendJoin(',',
                Quote(row.ExternalId),
                row.DonatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Quote(row.Donor),
                row.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                row.InKind ? "In-kind" : "Direct",
                row.NamedOnRequest == true ? "yes" : "no",
                Quote(row.Note ?? "")).Append('\n');
        }
        return output.ToString();
    }

    private static List<DonationRow> ParseDonorbox(string[] header, IEnumerable<string[]> lines)
    {
        var column = Columns(header, "Donorbox export");
        var receipt = column("Receipt Id");
        var donatedAt = column("Donated At");
        var name = column("Name");
        var company = column("Donating Company");
        var amount = column("Amount in USD");
        var fee = column("Total Fee");
        var net = column("Net amount in USD");
        var interval = column("Donation Interval");
        var type = column("Donation Type");
        var anonymous = column("Make Donation Anonymous");
        var status = column("Status");

        return lines
            .Select(Reader)
            .Where(cell => cell(status).Equals("paid", StringComparison.OrdinalIgnoreCase))
            .Select(cell => new DonationRow(
                cell(receipt),
                DateTime.SpecifyKind(DateTime.ParseExact(cell(donatedAt), "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Utc),
                Title(cell(company).Length > 0 ? cell(company) : cell(name)),
                Money(cell(amount)),
                Money(cell(fee)),
                Money(cell(net)),
                !cell(interval).Equals("One-time", StringComparison.OrdinalIgnoreCase),
                false,
                cell(type).Equals("paypal", StringComparison.OrdinalIgnoreCase) ? "PayPal" : "Stripe",
                cell(anonymous).Equals("yes", StringComparison.OrdinalIgnoreCase),
                null,
                null))
            .ToList();
    }

    private static List<DonationRow> ParseDirect(string[] header, IEnumerable<string[]> lines)
    {
        var column = Columns(header, $"Donorbox export or direct-donations file ({DirectHeader})");
        var id = column("Id");
        var date = column("Date");
        var donor = column("Donor");
        var amount = column("Amount");
        var type = column("Type");
        var isPublic = column("Public");
        var note = column("Note");

        return lines.Select(Reader).Select(cell =>
        {
            if (cell(id).Length == 0 || cell(donor).Length == 0)
            {
                throw new FormatException("Every row needs an Id and a Donor.");
            }

            if (!DateOnly.TryParseExact(cell(date), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                throw new FormatException($"Row {cell(id)}: dates are written 2026-01-31, not \"{cell(date)}\".");
            }

            var inKind = cell(type).Equals("In-kind", StringComparison.OrdinalIgnoreCase);
            if (!inKind && !cell(type).Equals("Direct", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException($"Row {cell(id)}: Type is Direct or In-kind, not \"{cell(type)}\".");
            }

            var value = Money(cell(amount));
            return new DonationRow(
                cell(id),
                day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                cell(donor),
                value,
                0m,
                inKind ? 0m : value,
                false,
                inKind,
                inKind ? "In-kind" : "Direct",
                false,
                cell(isPublic).Equals("yes", StringComparison.OrdinalIgnoreCase),
                cell(note).Length > 0 ? cell(note) : null);
        }).ToList();
    }

    private static Func<string, int> Columns(string[] header, string expected) => name =>
    {
        var index = Array.FindIndex(header, cell => cell.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : throw new FormatException($"That doesn't look like a {expected}: no \"{name}\" column.");
    };

    private static Func<int, string> Reader(string[] cells) => index => index < cells.Length ? cells[index].Trim() : "";

    private static decimal Money(string value) =>
        value.Length == 0 ? 0m : decimal.Parse(value.Replace("$", "").Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static string Quote(string value) =>
        value.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    // Donorbox keeps names as typed ("david mckendrick"); the public page shows them properly cased.
    private static string Title(string value) =>
        value.Any(char.IsUpper) ? value : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
}
