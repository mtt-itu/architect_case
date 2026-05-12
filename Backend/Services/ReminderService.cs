using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public record ReminderItem(int SubscriptionId, string ProviderName, string SubscriberNumber, string Period, DateOnly DueDate, int DaysUntilPayment);

public class ReminderService(AppDbContext db)
{
    public async Task<List<ReminderItem>> GetUnpaidSubscriptionsAsync(int customerId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var period = DateTime.Today.ToString("yyyy-MM");

        var subscriptions = await db.Subscriptions
            .Include(x => x.Payments)
            .Where(x => x.CustomerId == customerId && x.Status == SubscriptionStatus.Active)
            .ToListAsync();

        return subscriptions
            .Where(x => !x.Payments.Any(p => p.Period == period && p.Status == PaymentStatus.Successful))
            .Select(x =>
            {
                var dueDate = new DateOnly(today.Year, today.Month, Math.Clamp(x.PreferredPaymentDay, 1, DateTime.DaysInMonth(today.Year, today.Month)));
                var daysUntilPayment = dueDate.DayNumber - today.DayNumber;

                return new ReminderItem(x.Id, x.ProviderName, x.SubscriberNumber, period, dueDate, daysUntilPayment);
            })
            .Where(x => x.DaysUntilPayment <= 3)
            .ToList();
    }
}
