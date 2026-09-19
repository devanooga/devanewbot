namespace devanewbot.Seeders;

using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using Microsoft.AspNetCore.Identity;

public class RoleSeeder(RoleManager<Role> roleManager) : ISeeder
{
    public const string Administrators = "Administrators";

    public async Task Seed(CancellationToken cancellationToken = default)
    {
        if (await roleManager.FindByNameAsync(Administrators) is null)
        {
            await roleManager.CreateAsync(new Role { Name = Administrators });
        }
    }
}
