using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebtsController(AppDbContext db, DebtService debtService, AppDateService appDateService) : ControllerBase
{
    [HttpGet("subscription/{subscriptionId:int}")]
    public async Task<IActionResult> QueryDebt(int subscriptionId)
    {
        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId);
        if (subscription is null)
        {
            return NotFound();
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            var today = appDateService.Today;
            return Ok(new DebtResult(false, 0, today, $"{today.Year:D4}-{today.Month:D2}", "Pasif abonelik için borç sorgulanamaz."));
        }

        return Ok(debtService.QueryDebt(subscription));
    }
}
