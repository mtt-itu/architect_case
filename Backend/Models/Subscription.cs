namespace Backend.Models;

public class Subscription
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public SubscriptionType Type { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriberNumber { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public int BillingDay { get; set; } = 1;
    public int PreferredPaymentDay { get; set; } = 10;

    public Customer? Customer { get; set; }
    public List<Payment> Payments { get; set; } = [];
}
