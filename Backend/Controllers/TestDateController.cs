using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/test-date")]
public class TestDateController(AppDateService appDateService, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public ActionResult<AppDateInfo> Get()
    {
        return appDateService.GetInfo();
    }

    [HttpPost]
    public async Task<ActionResult<AppDateInfo>> Set(SetTestDateRequest request)
    {
        if (!await IsAdminRequest())
        {
            return Forbid();
        }

        return appDateService.Set(request.Date);
    }

    [HttpDelete]
    public async Task<ActionResult<AppDateInfo>> Reset()
    {
        if (!await IsAdminRequest())
        {
            return Forbid();
        }

        return appDateService.Reset();
    }

    private async Task<bool> IsAdminRequest()
    {
        if (!Request.Headers.TryGetValue("X-Admin-User-Id", out var value) ||
            !int.TryParse(value, out var adminId))
        {
            return false;
        }

        return await db.Customers.AnyAsync(x => x.Id == adminId && x.IsAdmin);
    }
}

public record SetTestDateRequest(DateOnly Date);
