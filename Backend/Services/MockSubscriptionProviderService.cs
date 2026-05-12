using Backend.Models;

namespace Backend.Services;

public record SubscriptionValidationResult(bool IsValid, int BillingDay, string Message);

public class MockSubscriptionProviderService
{
    public SubscriptionValidationResult Validate(SubscriptionType type, string providerName, string subscriberNumber)
    {
        if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(subscriberNumber))
        {
            return new SubscriptionValidationResult(false, 0, "Abonelik bilgileri eksik.");
        }

        if (subscriberNumber.Trim().Length < 4)
        {
            return new SubscriptionValidationResult(false, 0, "Abonelik numarasi en az 4 karakter olmali.");
        }

        var seed = Math.Abs(HashCode.Combine(type, providerName.Trim().ToLowerInvariant(), subscriberNumber.Trim()));
        var billingDay = 1 + seed % 28;

        return new SubscriptionValidationResult(true, billingDay, "Abonelik dogrulandi.");
    }
}
