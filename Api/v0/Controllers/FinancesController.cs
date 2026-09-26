namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using devanewbot.Services.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/api/v0/finances")]
[AllowAnonymous]
public class FinancesController(DevanewbotContext db) : ControllerBase
{
    private const string AnonymousDonor = "Individual donor";

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var publicPayees = (await db.FinancePayees.Where(payee => payee.IsPublic).Select(payee => payee.Name).ToListAsync()).ToHashSet();
        var accounts = await db.FinanceAccounts.AsNoTracking().OrderBy(account => account.Name).ToListAsync();
        var transactions = await db.FinanceTransactions
            .AsNoTracking()
            .Where(transaction => transaction.HiddenAt == null)
            .OrderByDescending(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Account)
            .ToListAsync();
        var donations = await db.Donations
            .AsNoTracking()
            .Where(donation => donation.HiddenAt == null)
            .OrderByDescending(donation => donation.DonatedAt)
            .ToListAsync();
        var named = DonationDisclosure.Reasons(donations);

        return Ok(new
        {
            Accounts = accounts.Select(account => new
            {
                account.Name,
                account.OpeningDate,
                account.OpeningBalance,
                Balance = account.OpeningBalance + transactions.Where(t => t.Account == account.Name).Sum(t => t.Amount),
                AsOf = transactions.Where(t => t.Account == account.Name).Select(t => (DateOnly?)t.Date).FirstOrDefault()
            }),
            Transactions = transactions.Select(transaction => new
            {
                transaction.Id,
                transaction.Date,
                transaction.Account,
                transaction.Kind,
                Payee = transaction.Kind == FinanceTransactionKind.Income && transaction.Payee is not null && !publicPayees.Contains(transaction.Payee)
                    ? AnonymousDonor
                    : transaction.Payee,
                transaction.Description,
                transaction.Category,
                transaction.Amount
            }),
            Donations = donations.Select(donation => new
            {
                donation.Id,
                Date = DateOnly.FromDateTime(donation.DonatedAt),
                Donor = named[donation] is null ? AnonymousDonor : donation.Donor,
                NamedBecause = named[donation],
                donation.Amount,
                donation.Fee,
                donation.Net,
                donation.Recurring,
                donation.InKind,
                donation.Source,
                donation.Note
            })
        });
    }
}
