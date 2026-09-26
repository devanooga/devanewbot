namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Admin;
using devanewbot.Data;
using devanewbot.Data.Models;
using devanewbot.Seeders;
using devanewbot.Services;
using devanewbot.Services.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/api/v0/admin/finances")]
[Authorize(Roles = RoleSeeder.Administrators)]
public class AdminFinancesController(DevanewbotContext db, FinanceImport financeImport, AdminIdentity adminIdentity) : ControllerBase
{
    protected DevanewbotContext Db { get; } = db;
    protected FinanceImport FinanceImport { get; } = financeImport;
    protected AdminIdentity AdminIdentity { get; } = adminIdentity;

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(new
        {
            Accounts = await Db.FinanceAccounts.AsNoTracking().OrderBy(account => account.Name).ToListAsync(),
            Payees = await Db.FinancePayees.AsNoTracking().OrderBy(payee => payee.Name).ToListAsync(),
            Transactions = await Db.FinanceTransactions
                .AsNoTracking()
                .OrderByDescending(transaction => transaction.Date)
                .ThenBy(transaction => transaction.Account)
                .ToListAsync()
        });

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] FinanceImportModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Csv))
        {
            return Error("Pick a QuickBooks CSV export.");
        }

        try
        {
            return Ok(await FinanceImport.Preview(model.Csv));
        }
        catch (FormatException e)
        {
            return Error(e.Message);
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] FinanceImportModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Csv))
        {
            return Error("Pick a QuickBooks CSV export.");
        }

        try
        {
            var (added, updated, hidden) = await FinanceImport.Apply(
                model.Csv,
                model.ApplyKeys.ToHashSet(),
                model.HideIds.ToHashSet(),
                await AdminIdentity.Name(User));
            return Ok(new { Added = added, Updated = updated, Hidden = hidden });
        }
        catch (FormatException e)
        {
            return Error(e.Message);
        }
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> AddAccount([FromBody] FinanceAccountModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || model.OpeningDate == default)
        {
            return Error("Give the account its QuickBooks name and an opening date.");
        }

        var name = model.Name.Trim();
        if (await Db.FinanceAccounts.AnyAsync(account => account.Name == name))
        {
            return Error($"{name} is already tracked.");
        }

        Db.FinanceAccounts.Add(new FinanceAccount { Name = name, OpeningDate = model.OpeningDate, OpeningBalance = model.OpeningBalance });
        await Db.SaveChangesAsync();
        return await List();
    }

    [HttpPut("accounts/{id}")]
    public async Task<IActionResult> UpdateAccount([FromRoute] Guid id, [FromBody] FinanceAccountModel model)
    {
        var account = await Db.FinanceAccounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        if (model.OpeningDate == default)
        {
            return Error("Give the account an opening date.");
        }

        account.OpeningDate = model.OpeningDate;
        account.OpeningBalance = model.OpeningBalance;
        await Db.SaveChangesAsync();
        return await List();
    }

    [HttpPut("payees/{id}")]
    public async Task<IActionResult> UpdatePayee([FromRoute] Guid id, [FromBody] FinancePayeeModel model)
    {
        var payee = await Db.FinancePayees.FindAsync(id);
        if (payee is null)
        {
            return NotFound();
        }

        payee.IsPublic = model.IsPublic;
        await Db.SaveChangesAsync();
        return await List();
    }

    [HttpPost("{id}/hide")]
    public async Task<IActionResult> Hide([FromRoute] Guid id) => await SetHidden(id, true);

    [HttpPost("{id}/unhide")]
    public async Task<IActionResult> Unhide([FromRoute] Guid id) => await SetHidden(id, false);

    private async Task<IActionResult> SetHidden(Guid id, bool hidden)
    {
        var transaction = await Db.FinanceTransactions.FindAsync(id);
        if (transaction is null)
        {
            return NotFound();
        }

        transaction.HiddenAt = hidden ? DateTime.UtcNow : null;
        transaction.HiddenBy = hidden ? await AdminIdentity.Name(User) : null;
        await Db.SaveChangesAsync();
        return await List();
    }

    private BadRequestObjectResult Error(string message) => BadRequest(new { Errors = new[] { message } });
}
