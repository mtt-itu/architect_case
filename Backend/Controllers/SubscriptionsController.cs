using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController(AppDbContext db, MockSubscriptionProviderService subscriptionProviderService) : ControllerBase
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

        var validation = await subscriptionProviderService.ValidateAsync(request.Type, request.ProviderName, request.SubscriberNumber);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Message);
        }

        var subscription = new Subscription
        {
            CustomerId = request.CustomerId,
            Type = request.Type,
            ProviderName = request.ProviderName,
            SubscriberNumber = request.SubscriberNumber,
            Status = request.Status,
            BillingDay = validation.BillingDay,
            PreferredPaymentDay = request.PreferredPaymentDay
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<SubscriptionValidationResult>> Validate(ValidateSubscriptionRequest request)
    {
        return await subscriptionProviderService.ValidateAsync(request.Type, request.ProviderName, request.SubscriberNumber);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSubscriptionRequest request)
    {
        var subscription = await db.Subscriptions.FindAsync(id);
        if (subscription is null)
        {
            return NotFound();
        }

        var validation = await subscriptionProviderService.ValidateAsync(request.Type, request.ProviderName, request.SubscriberNumber);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Message);
        }

        subscription.Type = request.Type;
        subscription.ProviderName = request.ProviderName;
        subscription.SubscriberNumber = request.SubscriberNumber;
        subscription.Status = request.Status;
        subscription.BillingDay = validation.BillingDay;
        subscription.PreferredPaymentDay = request.PreferredPaymentDay;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateSubscriptionStatusRequest request)
    {
        var subscription = await db.Subscriptions.FindAsync(id);
        if (subscription is null)
        {
            return NotFound();
        }

        subscription.Status = request.Status;
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
    int PreferredPaymentDay);

public record ValidateSubscriptionRequest(
    SubscriptionType Type,
    string ProviderName,
    string SubscriberNumber);

public record UpdateSubscriptionRequest(
    SubscriptionType Type,
    string ProviderName,
    string SubscriberNumber,
    SubscriptionStatus Status,
    int PreferredPaymentDay);

public record UpdateSubscriptionStatusRequest(SubscriptionStatus Status);
