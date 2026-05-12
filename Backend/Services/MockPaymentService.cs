namespace Backend.Services;

public class MockPaymentService
{
    public bool Pay(decimal amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        return Random.Shared.Next(1, 101) <= 90;
    }
}
