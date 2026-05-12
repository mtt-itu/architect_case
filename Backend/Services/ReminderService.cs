using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public record ReminderItem(int SubscriptionId, string ProviderName, string SubscriberNumber, string Period, DateOnly DueDate, int DaysUntilPayment);
public record SmsReminderItem(int SubscriptionId, string ProviderName, string SubscriberNumber, string PhoneNumber, string Message);
public record SmsReminderResult(int SentCount, string Message, List<SmsReminderItem> Items);

public class ReminderService(AppDbContext db, AppDateService appDateService)
{
    public async Task<List<ReminderItem>> GetUnpaidSubscriptionsAsync(int customerId)
    {
        var today = appDateService.Today;

        var subscriptions = await db.Subscriptions
            .Include(x => x.Payments)
            .Where(x => x.CustomerId == customerId && x.Status == SubscriptionStatus.Active)
            .ToListAsync();

        return subscriptions
            .Select(x =>
            {
                var cycle = GetCurrentBillingCycle(today, x.BillingDay, x.PreferredPaymentDay);
                var daysUntilPayment = cycle.PaymentDate.DayNumber - today.DayNumber;

                return new { Subscription = x, Cycle = cycle, DaysUntilPayment = daysUntilPayment };
            })
            .Where(x => !x.Subscription.Payments.Any(p => p.Period == x.Cycle.Period && p.Status == PaymentStatus.Successful))
            .Where(x => x.DaysUntilPayment <= 3)
            .Select(x => new ReminderItem(
                x.Subscription.Id,
                x.Subscription.ProviderName,
                x.Subscription.SubscriberNumber,
                x.Cycle.Period,
                x.Cycle.PaymentDate,
                x.DaysUntilPayment))
            .ToList();
    }

    public async Task<SmsReminderResult> SendSmsRemindersAsync(int customerId)
    {
        var today = appDateService.Today;

        var subscriptions = await db.Subscriptions
            .Include(x => x.Customer)
            .Include(x => x.Payments)
            .Where(x => x.CustomerId == customerId && x.Status == SubscriptionStatus.Active)
            .ToListAsync();

        var items = subscriptions
            .Select(x =>
            {
                var cycle = GetCurrentBillingCycle(today, x.BillingDay, x.PreferredPaymentDay);
                return new { Subscription = x, Cycle = cycle, DaysUntilPayment = cycle.PaymentDate.DayNumber - today.DayNumber };
            })
            .Where(x => !x.Subscription.Payments.Any(p => p.Period == x.Cycle.Period && p.Status == PaymentStatus.Successful))
            .Where(x => x.DaysUntilPayment == 1)
            .Select(x => new SmsReminderItem(
                x.Subscription.Id,
                x.Subscription.ProviderName,
                x.Subscription.SubscriberNumber,
                x.Subscription.Customer?.PhoneNumber ?? string.Empty,
                $"Sayın müşterimiz, {x.Subscription.ProviderName} aboneliğinizin ödeme günü yarındır. Dönem: {x.Cycle.Period}."))
            .ToList();

        var message = items.Count == 0
            ? "Bugün SMS gönderilecek ödeme hatırlatması yok."
            : items.Count == 1
                ? "1 SMS hatırlatması gönderildi."
                : $"{items.Count} SMS hatırlatması gönderildi.";

        return new SmsReminderResult(items.Count, message, items);
    }

    private static BillingCycle GetCurrentBillingCycle(DateOnly today, int billingDay, int preferredPaymentDay)
    {
        var currentBillingDay = Math.Clamp(billingDay, 1, DateTime.DaysInMonth(today.Year, today.Month));
        var periodMonth = today.Day >= currentBillingDay
            ? new DateOnly(today.Year, today.Month, 1)
            : new DateOnly(today.Year, today.Month, 1).AddMonths(-1);

        var paymentMonth = preferredPaymentDay >= billingDay ? periodMonth : periodMonth.AddMonths(1);
        var paymentDate = new DateOnly(
            paymentMonth.Year,
            paymentMonth.Month,
            Math.Clamp(preferredPaymentDay, 1, DateTime.DaysInMonth(paymentMonth.Year, paymentMonth.Month)));

        return new BillingCycle($"{periodMonth.Year:D4}-{periodMonth.Month:D2}", paymentDate);
    }

    private record BillingCycle(string Period, DateOnly PaymentDate);
}
