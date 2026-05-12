using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebtsController(AppDbContext db, DebtService debtService) : ControllerBase
{
    [HttpGet("subscription/{subscriptionId:int}")]
    public async Task<IActionResult> QueryDebt(int subscriptionId)
    {
        var subscription = await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == subscriptionId);
        if (subscription is null)
        {
            return NotFound();
        }

        return Ok(debtService.QueryDebt(subscription));
    }
}
