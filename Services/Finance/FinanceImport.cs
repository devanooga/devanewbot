namespace devanewbot.Services.Finance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.EntityFrameworkCore;

[JsonConverter(typeof(JsonStringEnumConverter<ImportStatus>))]
public enum ImportStatus
{
    New,
    Changed,
    Unchanged
}

public record ImportRow(
    string Key,
    ImportStatus Status,
    DateOnly Date,
    string Account,
    FinanceTransactionKind Kind,
    string? Payee,
    string? Description,
    string Category,
    decimal Amount,
    IReadOnlyList<string> Changes);

public record ImportPreview(
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<ImportRow> Rows,
    IReadOnlyList<FinanceTransaction> Missing,
    IReadOnlyList<string> SkippedAccounts,
    int SkippedBeforeOpening);

public partial class FinanceImport(DevanewbotContext db)
{
    private const string RemovedSuffix = " (not in QuickBooks export)";

    [GeneratedRegex(@"\s*\[bank:([^\]]+)\]\s*")]
    private static partial Regex BankMarker { get; }

    public async Task<ImportPreview> Preview(string csv)
    {
        var accounts = await db.FinanceAccounts.AsNoTracking().ToDictionaryAsync(account => account.Name, StringComparer.OrdinalIgnoreCase);
        var parsed = QuickBooksCsv.Parse(csv);

        var skippedAccounts = parsed
            .Select(row => row.Account)
            .Where(name => !accounts.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();
        var tracked = parsed.Where(row => accounts.ContainsKey(row.Account)).ToList();
        var inRange = tracked.Where(row => row.Date >= accounts[row.Account].OpeningDate).ToList();

        var incoming = inRange
            .Select(row => ToImportRow(row, accounts))
            .GroupBy(row => row.Key)
            .Select(group => group.First())
            .ToList();

        if (incoming.Count == 0)
        {
            return new ImportPreview(null, null, [], [], skippedAccounts, tracked.Count - inRange.Count);
        }

        var from = incoming.Min(row => row.Date);
        var to = incoming.Max(row => row.Date);
        var accountNames = incoming.Select(row => row.Account).Distinct().ToList();
        var existing = await db.FinanceTransactions
            .AsNoTracking()
            .Where(transaction => accountNames.Contains(transaction.Account) && transaction.Date >= from && transaction.Date <= to)
            .ToListAsync();
        var existingByKey = existing.ToDictionary(transaction => transaction.ExternalKey);
        var keysInFile = incoming.Select(row => row.Key).ToHashSet();

        var rows = incoming
            .Select(row => existingByKey.TryGetValue(row.Key, out var current) ? Compare(row, current) : row)
            .OrderBy(row => row.Date)
            .ThenBy(row => row.Account)
            .ToList();
        var missing = existing
            .Where(transaction => transaction.HiddenAt == null && !keysInFile.Contains(transaction.ExternalKey))
            .OrderBy(transaction => transaction.Date)
            .ToList();

        return new ImportPreview(from, to, rows, missing, skippedAccounts, tracked.Count - inRange.Count);
    }

    public async Task<(int Added, int Updated, int Hidden)> Apply(string csv, IReadOnlySet<string> keys, IReadOnlySet<Guid> hideIds, string administrator)
    {
        var preview = await Preview(csv);
        var chosen = preview.Rows.Where(row => row.Status != ImportStatus.Unchanged && keys.Contains(row.Key)).ToList();
        var chosenKeys = chosen.Select(row => row.Key).ToList();
        var current = await db.FinanceTransactions
            .Where(transaction => chosenKeys.Contains(transaction.ExternalKey))
            .ToDictionaryAsync(transaction => transaction.ExternalKey);

        int added = 0, updated = 0;
        foreach (var row in chosen)
        {
            if (!current.TryGetValue(row.Key, out var transaction))
            {
                transaction = new FinanceTransaction
                {
                    ExternalKey = row.Key,
                    Account = row.Account,
                    Category = row.Category
                };
                db.FinanceTransactions.Add(transaction);
                added++;
            }
            else
            {
                updated++;
            }

            if (transaction.HiddenBy?.EndsWith(RemovedSuffix) == true)
            {
                transaction.HiddenAt = null;
                transaction.HiddenBy = null;
            }

            transaction.Date = row.Date;
            transaction.Account = row.Account;
            transaction.Kind = row.Kind;
            transaction.Payee = row.Payee;
            transaction.Description = row.Description;
            transaction.Category = row.Category;
            transaction.Amount = row.Amount;
        }

        var missingIds = preview.Missing.Select(transaction => transaction.Id).Where(hideIds.Contains).ToList();
        var toHide = await db.FinanceTransactions.Where(transaction => missingIds.Contains(transaction.Id)).ToListAsync();
        foreach (var transaction in toHide)
        {
            transaction.HiddenAt = DateTime.UtcNow;
            transaction.HiddenBy = administrator + RemovedSuffix;
        }

        var incomePayees = chosen
            .Where(row => row.Kind == FinanceTransactionKind.Income && row.Payee is not null)
            .Select(row => row.Payee!)
            .Distinct()
            .ToList();
        var knownPayees = await db.FinancePayees.Where(payee => incomePayees.Contains(payee.Name)).Select(payee => payee.Name).ToListAsync();
        db.FinancePayees.AddRange(incomePayees.Except(knownPayees).Select(name => new FinancePayee { Name = name }));

        await db.SaveChangesAsync();
        return (added, updated, toHide.Count);
    }

    private static ImportRow ToImportRow(QuickBooksRow row, IReadOnlyDictionary<string, FinanceAccount> accounts)
    {
        var marker = BankMarker.Match(row.Memo);
        var account = accounts[row.Account].Name;
        var identity = marker.Success
            ? marker.Groups[1].Value
            : Fingerprint($"{row.Type}|{row.Date:yyyy-MM-dd}|{row.Number}|{row.Name}|{row.Amount:0.00}");
        var description = BankMarker.Replace(row.Memo, " ").Trim();
        var isTransfer = accounts.ContainsKey(row.Split);

        return new ImportRow(
            $"{account}|{identity}",
            ImportStatus.New,
            row.Date,
            account,
            isTransfer ? FinanceTransactionKind.Transfer : row.Amount >= 0 ? FinanceTransactionKind.Income : FinanceTransactionKind.Expense,
            row.Name.Length > 0 ? row.Name : null,
            description.Length > 0 ? description : null,
            isTransfer ? "Transfer" : row.Split is "" or "-SPLIT-" ? "Multiple categories" : row.Split,
            row.Amount,
            []);
    }

    private static ImportRow Compare(ImportRow row, FinanceTransaction current)
    {
        var changes = new List<string>();
        if (row.Date != current.Date) changes.Add("date");
        if (row.Amount != current.Amount) changes.Add("amount");
        if (row.Kind != current.Kind) changes.Add("kind");
        if (row.Payee != current.Payee) changes.Add("payee");
        if (row.Description != current.Description) changes.Add("description");
        if (row.Category != current.Category) changes.Add("category");
        if (current.HiddenBy?.EndsWith(RemovedSuffix) == true) changes.Add("back in QuickBooks");
        return row with { Status = changes.Count == 0 ? ImportStatus.Unchanged : ImportStatus.Changed, Changes = changes };
    }

    private static string Fingerprint(string value) =>
        "qb-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
}
