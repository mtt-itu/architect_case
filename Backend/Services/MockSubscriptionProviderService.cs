using System.Net.Http.Json;
using Backend.Models;

namespace Backend.Services;

public record SubscriptionValidationResult(bool IsValid, int BillingDay, string Message);
public record MockSubscriptionValidationRequest(
    SubscriptionType Type,
    string ProviderName,
    string SubscriberNumber);

public class MockSubscriptionProviderService(HttpClient httpClient)
{
    public async Task<SubscriptionValidationResult> ValidateAsync(SubscriptionType type, string providerName, string subscriberNumber)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/mock-subscription-provider/validate",
            new MockSubscriptionValidationRequest(type, providerName, subscriberNumber));
        var result = await response.Content.ReadFromJsonAsync<SubscriptionValidationResult>();

        if (result is not null)
        {
            return result;
        }

        return new SubscriptionValidationResult(false, 0, "Abonelik servisi cevap vermedi.");
    }
}
