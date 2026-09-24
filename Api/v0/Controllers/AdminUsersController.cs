namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Admin;
using devanewbot.Data.Models;
using devanewbot.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/api/v0/admin/users")]
[Authorize(Roles = RoleSeeder.Administrators)]
public class AdminUsersController(UserManager<User> userManager) : ControllerBase
{
    protected UserManager<User> UserManager { get; } = userManager;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await Users());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserModel model)
    {
        var user = new User { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
        var result = await UserManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return Errors(result);
        }

        await UserManager.AddToRoleAsync(user, RoleSeeder.Administrators);
        return Ok(await Users());
    }

    [HttpPost("{id}/password")]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id, [FromBody] PasswordResetModel model)
    {
        var user = await UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var token = await UserManager.GeneratePasswordResetTokenAsync(user);
        var result = await UserManager.ResetPasswordAsync(user, token, model.NewPassword);
        return result.Succeeded ? Ok() : Errors(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var user = await UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var current = await UserManager.GetUserAsync(User);
        if (current?.Id == user.Id)
        {
            return BadRequest(new { Errors = new[] { "You cannot delete the account you are signed in with." } });
        }

        var result = await UserManager.DeleteAsync(user);
        return result.Succeeded ? Ok(await Users()) : Errors(result);
    }

    private async Task<object> Users() =>
        await UserManager.Users
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email, user.CreatedAt, user.LockoutEnd, user.AccessFailedCount })
            .ToListAsync();

    private BadRequestObjectResult Errors(IdentityResult result) =>
        BadRequest(new { Errors = result.Errors.Select(error => error.Description) });
}
