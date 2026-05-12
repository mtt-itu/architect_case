using Backend.Models;

namespace Backend.Services;

public record DebtResult(bool HasDebt, decimal Amount, DateOnly DueDate, string Period, string Message);

public class DebtService(AppDateService appDateService)
{
    public DebtResult QueryDebt(Subscription subscription)
    {
        var today = appDateService.Today;
        var period = $"{today.Year:D4}-{today.Month:D2}";
        var billingDay = Math.Clamp(subscription.BillingDay, 1, DateTime.DaysInMonth(today.Year, today.Month));
        var billingDate = new DateOnly(today.Year, today.Month, billingDay);

        if (subscription.Payments.Any(x => x.Period == period && x.Status == PaymentStatus.Successful))
        {
            return new DebtResult(false, 0, billingDate, period, "Bu abonelik için bu ay ödeme yapılmış.");
        }

        if (today < billingDate)
        {
            return new DebtResult(false, 0, billingDate, period, "Bu abonelik için fatura kesim tarihi henüz gelmedi.");
        }

        var seed = Math.Abs(HashCode.Combine(subscription.ProviderName, subscription.SubscriberNumber, period));
        var amount = 100 + seed % 900;

        return new DebtResult(true, amount, billingDate, period, "Borç sorgulandı.");
    }
}
