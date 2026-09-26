namespace devanewbot.Services.Finance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.EntityFrameworkCore;

public record DonationImportRow(
    string Key,
    ImportStatus Status,
    DateTime DonatedAt,
    string Donor,
    decimal Amount,
    decimal Fee,
    decimal Net,
    bool Recurring,
    bool InKind,
    string Source,
    bool? NamedOnRequest,
    string? Note,
    IReadOnlyList<string> Changes);

public record DonationImportPreview(DateTime? From, DateTime? To, IReadOnlyList<DonationImportRow> Rows, IReadOnlyList<Donation> Missing);

public class DonationImport(DevanewbotContext db)
{
    private const string RemovedSuffix = " (not in the uploaded file)";
    private static readonly string[] DirectSources = ["Direct", "In-kind"];

    public async Task<DonationImportPreview> Preview(string csv)
    {
        var incoming = DonationCsv.Parse(csv).GroupBy(row => row.ExternalId).Select(group => group.First()).ToList();
        if (incoming.Count == 0)
        {
            return new DonationImportPreview(null, null, [], []);
        }

        var from = incoming.Min(row => row.DonatedAt);
        var to = incoming.Max(row => row.DonatedAt);
        // A Donorbox export says nothing about direct gifts, and the direct file says nothing about Donorbox ones.
        var direct = incoming[0].NamedOnRequest is not null;
        var existing = await db.Donations
            .AsNoTracking()
            .Where(donation => donation.DonatedAt >= from && donation.DonatedAt <= to)
            .Where(donation => DirectSources.Contains(donation.Source) == direct)
            .ToListAsync();
        var existingById = existing.ToDictionary(donation => donation.ExternalId);
        var ids = incoming.Select(row => row.ExternalId).ToHashSet();

        var rows = incoming
            .Select(row => existingById.TryGetValue(row.ExternalId, out var current) ? Compare(row, current) : ToImportRow(row, ImportStatus.New, []))
            .OrderBy(row => row.DonatedAt)
            .ToList();
        var missing = existing.Where(donation => donation.HiddenAt == null && !ids.Contains(donation.ExternalId)).OrderBy(donation => donation.DonatedAt).ToList();
        return new DonationImportPreview(from, to, rows, missing);
    }

    public async Task<(int Added, int Updated, int Hidden)> Apply(string csv, IReadOnlySet<string> keys, IReadOnlySet<Guid> hideIds, string administrator)
    {
        var preview = await Preview(csv);
        var byId = DonationCsv.Parse(csv).GroupBy(row => row.ExternalId).ToDictionary(group => group.Key, group => group.First());
        var chosen = preview.Rows.Where(row => row.Status != ImportStatus.Unchanged && keys.Contains(row.Key)).ToList();
        var chosenIds = chosen.Select(row => row.Key).ToList();
        var current = await db.Donations.Where(donation => chosenIds.Contains(donation.ExternalId)).ToDictionaryAsync(donation => donation.ExternalId);

        int added = 0, updated = 0;
        foreach (var row in chosen.Select(row => byId[row.Key]))
        {
            if (!current.TryGetValue(row.ExternalId, out var donation))
            {
                donation = new Donation { ExternalId = row.ExternalId, Donor = row.Donor, Source = row.Source };
                db.Donations.Add(donation);
                added++;
            }
            else
            {
                updated++;
            }

            if (donation.HiddenBy?.EndsWith(RemovedSuffix) == true)
            {
                donation.HiddenAt = null;
                donation.HiddenBy = null;
            }

            donation.DonatedAt = row.DonatedAt;
            donation.Donor = row.Donor;
            donation.Amount = row.Amount;
            donation.Fee = row.Fee;
            donation.Net = row.Net;
            donation.Recurring = row.Recurring;
            donation.InKind = row.InKind;
            donation.Source = row.Source;
            donation.Note = row.Note;
            donation.AnonymousRequested = row.AnonymousRequested;
            if (row.NamedOnRequest is { } named)
            {
                donation.NamedOnRequest = named;
            }
        }

        var missingIds = preview.Missing.Select(donation => donation.Id).Where(hideIds.Contains).ToList();
        var toHide = await db.Donations.Where(donation => missingIds.Contains(donation.Id)).ToListAsync();
        foreach (var donation in toHide)
        {
            donation.HiddenAt = DateTime.UtcNow;
            donation.HiddenBy = administrator + RemovedSuffix;
        }

        await db.SaveChangesAsync();
        return (added, updated, toHide.Count);
    }

    private static DonationImportRow ToImportRow(DonationRow row, ImportStatus status, IReadOnlyList<string> changes) =>
        new(row.ExternalId, status, row.DonatedAt, row.Donor, row.Amount, row.Fee, row.Net, row.Recurring, row.InKind, row.Source, row.NamedOnRequest, row.Note, changes);

    private static DonationImportRow Compare(DonationRow row, Donation current)
    {
        var changes = new List<string>();
        if (row.DonatedAt != current.DonatedAt) changes.Add("date");
        if (row.Donor != current.Donor) changes.Add("donor");
        if (row.Amount != current.Amount || row.Fee != current.Fee || row.Net != current.Net) changes.Add("amount");
        if (row.Recurring != current.Recurring) changes.Add("recurring");
        if (row.InKind != current.InKind) changes.Add("type");
        if (row.Note != current.Note) changes.Add("note");
        if (row.NamedOnRequest is { } named && named != current.NamedOnRequest) changes.Add("public");
        if (row.AnonymousRequested != current.AnonymousRequested) changes.Add("anonymous request");
        if (current.HiddenBy?.EndsWith(RemovedSuffix) == true) changes.Add("back in Donorbox");
        return ToImportRow(row, changes.Count == 0 ? ImportStatus.Unchanged : ImportStatus.Changed, changes);
    }
}
