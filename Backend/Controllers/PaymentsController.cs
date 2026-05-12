using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(AppDbContext db, DebtService debtService, MockPaymentService paymentService) : ControllerBase
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
    public async Task<ActionResult<List<CustomerPaymentResponse>>> GetByCustomer(int customerId)
    {
        return await db.Payments
            .AsNoTracking()
            .Where(x => x.Subscription != null && x.Subscription.CustomerId == customerId)
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => new CustomerPaymentResponse(
                x.Id,
                x.SubscriptionId,
                x.Subscription!.Type,
                x.Subscription.ProviderName,
                x.Subscription.SubscriberNumber,
                x.Amount,
                x.PaymentDate,
                x.Period,
                x.Status))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> Create(CreatePaymentRequest request)
    {
        var subscription = await db.Subscriptions
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == request.SubscriptionId);

        if (subscription is null)
        {
            return BadRequest("Subscription not found.");
        }

        var debt = debtService.QueryDebt(subscription);
        if (!debt.HasDebt)
        {
            return BadRequest(debt.Message);
        }

        var isSuccessful = paymentService.Pay(debt.Amount);
        var payment = new Payment
        {
            SubscriptionId = request.SubscriptionId,
            Amount = debt.Amount,
            Period = debt.Period,
            PaymentDate = DateTime.UtcNow,
            Status = isSuccessful ? PaymentStatus.Successful : PaymentStatus.Failed
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBySubscription), new { subscriptionId = payment.SubscriptionId }, payment);
    }
}

public record CreatePaymentRequest(int SubscriptionId, decimal Amount, string Period);

public record CustomerPaymentResponse(
    int Id,
    int SubscriptionId,
    SubscriptionType SubscriptionType,
    string ProviderName,
    string SubscriberNumber,
    decimal Amount,
    DateTime PaymentDate,
    string Period,
    PaymentStatus Status);
