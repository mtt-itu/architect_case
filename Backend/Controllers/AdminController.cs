using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
    [HttpDelete("customers/{id:int}/hard-delete")]
    public async Task<IActionResult> HardDeleteCustomer(int id)
    {
        var admin = await GetAdminCustomer();
        if (admin is null)
        {
            return Forbid();
        }

        if (admin.Id == id)
        {
            return BadRequest("Aktif admin kullanıcısı kalıcı olarak silinemez.");
        }

        var customer = await db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (customer is null)
        {
            return NotFound();
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<Backend.Models.Customer?> GetAdminCustomer()
    {
        if (!Request.Headers.TryGetValue("X-Admin-User-Id", out var value) ||
            !int.TryParse(value, out var adminId))
        {
            return null;
        }

        return await db.Customers.FirstOrDefaultAsync(x => x.Id == adminId && x.IsAdmin);
    }
}
