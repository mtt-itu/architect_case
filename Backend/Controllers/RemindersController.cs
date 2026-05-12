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

    [HttpPost("customer/{customerId:int}/send-sms")]
    public async Task<IActionResult> SendSmsForCustomer(int customerId)
    {
        return Ok(await reminderService.SendSmsRemindersAsync(customerId));
    }
}
