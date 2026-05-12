using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/mock-payment-provider")]
public class MockPaymentProviderController : ControllerBase
{
    [HttpPost("pay")]
    public ActionResult<PaymentServiceResult> Pay(MockPaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new PaymentServiceResult(false, "Odeme tutari gecersiz."));
        }

        var isSuccessful = Random.Shared.Next(1, 101) <= 95;

        return isSuccessful
            ? new PaymentServiceResult(true, "Odeme basarili.")
            : new PaymentServiceResult(false, "Odeme basarisiz oldu. Lutfen tekrar deneyin.");
    }
}
