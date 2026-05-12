using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(AppDbContext db, MockPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Payment>>> GetAll()
    {
        return await db.Payments.AsNoTracking().OrderByDescending(x => x.PaymentDate).ToListAsync();
    }

    [HttpGet("subscription/{subscriptionId:int}")]
    public async Task<ActionResult<List<Payment>>> GetBySubscription(int subscriptionId)
    {
        return await db.Payments
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<List<Payment>>> GetByCustomer(int customerId)
    {
        return await db.Payments
            .AsNoTracking()
            .Where(x => x.Subscription != null && x.Subscription.CustomerId == customerId)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> Create(CreatePaymentRequest request)
    {
        var subscriptionExists = await db.Subscriptions.AnyAsync(x => x.Id == request.SubscriptionId);
        if (!subscriptionExists)
        {
            return BadRequest("Subscription not found.");
        }

        var isSuccessful = paymentService.Pay(request.Amount);
        var payment = new Payment
        {
            SubscriptionId = request.SubscriptionId,
            Amount = request.Amount,
            Period = request.Period,
            PaymentDate = DateTime.UtcNow,
            Status = isSuccessful ? PaymentStatus.Successful : PaymentStatus.Failed
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBySubscription), new { subscriptionId = payment.SubscriptionId }, payment);
    }
}

public record CreatePaymentRequest(int SubscriptionId, decimal Amount, string Period);
