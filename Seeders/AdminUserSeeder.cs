namespace devanewbot.Seeders;

using System;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class AdminUserSeeder(
    DevanewbotContext db,
    UserManager<User> userManager,
    IConfiguration configuration,
    ILogger<AdminUserSeeder> logger) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var email = configuration.GetValue<string>("Admin:Email");
        var password = configuration.GetValue<string>("Admin:Password");
        if (string.IsNullOrWhiteSpace(email) || email.StartsWith("#{") || string.IsNullOrWhiteSpace(password) || password.StartsWith("#{"))
        {
            logger.LogWarning("No users exist and Admin:Email / Admin:Password are not set, so no admin account was seeded");
            return;
        }

        var admin = new User { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not seed admin user: {string.Join("; ", result.Errors)}");
        }

        await userManager.AddToRoleAsync(admin, RoleSeeder.Administrators);
        logger.LogInformation("Seeded admin user {Email}", email);
    }
}
