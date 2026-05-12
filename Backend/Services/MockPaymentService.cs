namespace Backend.Services;

public record PaymentServiceResult(bool IsSuccessful, string Message);

public class MockPaymentService
{
    public PaymentServiceResult Pay(decimal amount)
    {
        if (amount <= 0)
        {
            return new PaymentServiceResult(false, "Odeme tutari gecersiz.");
        }

        var isSuccessful = Random.Shared.Next(1, 101) <= 95;

        return isSuccessful
            ? new PaymentServiceResult(true, "Odeme basarili.")
            : new PaymentServiceResult(false, "Odeme basarisiz oldu. Lutfen tekrar deneyin.");
    }
}
