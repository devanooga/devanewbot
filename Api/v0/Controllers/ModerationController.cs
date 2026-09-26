namespace devanewbot.Api.v0.Controllers;

using System.Linq;
using System.Threading.Tasks;
using devanewbot.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/api/v0/moderation")]
[AllowAnonymous]
public class ModerationController(DevanewbotContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.ModerationActions
            .OrderByDescending(action => action.OccurredAt)
            .Select(action => new { action.Id, action.OccurredAt, action.Action, action.Reason, action.Administrator })
            .ToListAsync());
}
