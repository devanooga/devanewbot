namespace devanewbot.Seeders;

using System;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.EntityFrameworkCore;

// The public ledger starts in 2020; these are the QuickBooks balances as of that morning.
public class FinanceAccountSeeder(DevanewbotContext db) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        if (await db.FinanceAccounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var opening = new DateOnly(2020, 1, 1);
        db.FinanceAccounts.AddRange(
            new FinanceAccount { Name = "Ally Bank", OpeningDate = opening, OpeningBalance = 40.78m },
            new FinanceAccount { Name = "Middlesex Federal Savings", OpeningDate = opening, OpeningBalance = 0m });
        db.FinancePayees.AddRange(
            new FinancePayee { Name = "Stripe", IsPublic = true },
            new FinancePayee { Name = "Amazon Smile", IsPublic = true },
            new FinancePayee { Name = "PayPal", IsPublic = true });
        await db.SaveChangesAsync(cancellationToken);
    }
}
