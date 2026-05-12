using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RemindersController(ReminderService reminderService) : ControllerBase
{
    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetForCustomer(int customerId)
    {
        return Ok(await reminderService.GetUnpaidSubscriptionsAsync(customerId));
    }
}
