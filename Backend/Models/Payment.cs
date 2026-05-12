namespace Backend.Models;

public class Payment
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string Period { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }

    public Subscription? Subscription { get; set; }
}
