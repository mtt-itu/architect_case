using System.Net.Http.Json;

namespace Backend.Services;

public record PaymentServiceResult(bool IsSuccessful, string Message);
public record MockPaymentRequest(decimal Amount);

public class MockPaymentService(HttpClient httpClient)
{
    public async Task<PaymentServiceResult> PayAsync(decimal amount)
    {
        var response = await httpClient.PostAsJsonAsync("api/mock-payment-provider/pay", new MockPaymentRequest(amount));
        var result = await response.Content.ReadFromJsonAsync<PaymentServiceResult>();

        if (result is not null)
        {
            return result;
        }

        return new PaymentServiceResult(false, "Ödeme servisi cevap vermedi.");
    }
}
