using Backend.Models;

namespace Backend.Services;

public record DebtResult(bool HasDebt, decimal Amount, DateOnly DueDate, string Period, string Message);

public class DebtService(AppDateService appDateService)
{
    public DebtResult QueryDebt(Subscription subscription)
    {
        var today = appDateService.Today;
        var cycle = GetCurrentBillingCycle(today, subscription.BillingDay, subscription.PreferredPaymentDay);

        if (subscription.Payments.Any(x => x.Period == cycle.Period && x.Status == PaymentStatus.Successful))
        {
            return new DebtResult(false, 0, cycle.BillingDate, cycle.Period, "Bu dönem için ödeme yapılmış.");
        }

        if (today < cycle.BillingDate)
        {
            return new DebtResult(false, 0, cycle.BillingDate, cycle.Period, "Bu abonelik için fatura kesim tarihi henüz gelmedi.");
        }

        var seed = Math.Abs(HashCode.Combine(subscription.ProviderName, subscription.SubscriberNumber, cycle.Period));
        var amount = 100 + seed % 900;

        return new DebtResult(true, amount, cycle.BillingDate, cycle.Period, "Borç sorgulandı.");
    }

    private static BillingCycle GetCurrentBillingCycle(DateOnly today, int billingDay, int preferredPaymentDay)
    {
        var currentBillingDay = Math.Clamp(billingDay, 1, DateTime.DaysInMonth(today.Year, today.Month));
        var periodMonth = today.Day >= currentBillingDay
            ? new DateOnly(today.Year, today.Month, 1)
            : new DateOnly(today.Year, today.Month, 1).AddMonths(-1);

        var billingDate = new DateOnly(
            periodMonth.Year,
            periodMonth.Month,
            Math.Clamp(billingDay, 1, DateTime.DaysInMonth(periodMonth.Year, periodMonth.Month)));
        var paymentMonth = preferredPaymentDay >= billingDay ? periodMonth : periodMonth.AddMonths(1);
        var paymentDate = new DateOnly(
            paymentMonth.Year,
            paymentMonth.Month,
            Math.Clamp(preferredPaymentDay, 1, DateTime.DaysInMonth(paymentMonth.Year, paymentMonth.Month)));

        return new BillingCycle($"{periodMonth.Year:D4}-{periodMonth.Month:D2}", billingDate, paymentDate);
    }

    private record BillingCycle(string Period, DateOnly BillingDate, DateOnly PaymentDate);
}
