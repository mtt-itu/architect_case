using Backend.Models;

namespace Backend.Services;

public record DebtResult(decimal Amount, DateOnly DueDate, string Period);

public class DebtService
{
    public DebtResult QueryDebt(Subscription subscription)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var period = DateTime.Today.ToString("yyyy-MM");
        var seed = Math.Abs(HashCode.Combine(subscription.ProviderName, subscription.SubscriberNumber, period));
        var amount = 100 + seed % 900;
        var dueDay = Math.Clamp(subscription.PaymentDueDay, 1, DateTime.DaysInMonth(today.Year, today.Month));

        return new DebtResult(amount, new DateOnly(today.Year, today.Month, dueDay), period);
    }
}
