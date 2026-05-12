using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Subscription>>> GetAll()
    {
        return await db.Subscriptions.AsNoTracking().ToListAsync();
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<List<Subscription>>> GetByCustomer(int customerId)
    {
        return await db.Subscriptions
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Subscription>> GetById(int id)
    {
        var subscription = await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return subscription is null ? NotFound() : subscription;
    }

    [HttpPost]
    public async Task<ActionResult<Subscription>> Create(CreateSubscriptionRequest request)
    {
        var customerExists = await db.Customers.AnyAsync(x => x.Id == request.CustomerId);
        if (!customerExists)
        {
            return BadRequest("Customer not found.");
        }

        var subscription = new Subscription
        {
            CustomerId = request.CustomerId,
            Type = request.Type,
            ProviderName = request.ProviderName,
            SubscriberNumber = request.SubscriberNumber,
            Status = request.Status,
            PaymentDueDay = request.PaymentDueDay
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSubscriptionRequest request)
    {
        var subscription = await db.Subscriptions.FindAsync(id);
        if (subscription is null)
        {
            return NotFound();
        }

        subscription.Type = request.Type;
        subscription.ProviderName = request.ProviderName;
        subscription.SubscriberNumber = request.SubscriberNumber;
        subscription.Status = request.Status;
        subscription.PaymentDueDay = request.PaymentDueDay;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subscription = await db.Subscriptions.FindAsync(id);
        if (subscription is null)
        {
            return NotFound();
        }

        db.Subscriptions.Remove(subscription);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateSubscriptionRequest(
    int CustomerId,
    SubscriptionType Type,
    string ProviderName,
    string SubscriberNumber,
    SubscriptionStatus Status,
    int PaymentDueDay);

public record UpdateSubscriptionRequest(
    SubscriptionType Type,
    string ProviderName,
    string SubscriberNumber,
    SubscriptionStatus Status,
    int PaymentDueDay);
