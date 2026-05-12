using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/mock-subscription-provider")]
public class MockSubscriptionProviderController : ControllerBase
{
    [HttpPost("validate")]
    public ActionResult<SubscriptionValidationResult> Validate(MockSubscriptionValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderName) || string.IsNullOrWhiteSpace(request.SubscriberNumber))
        {
            return BadRequest(new SubscriptionValidationResult(false, 0, "Abonelik bilgileri eksik."));
        }

        if (request.SubscriberNumber.Trim().Length < 4)
        {
            return BadRequest(new SubscriptionValidationResult(false, 0, "Abonelik numarasi en az 4 karakter olmali."));
        }

        var seed = Math.Abs(HashCode.Combine(
            request.Type,
            request.ProviderName.Trim().ToLowerInvariant(),
            request.SubscriberNumber.Trim()));
        var billingDay = 1 + seed % 28;

        return new SubscriptionValidationResult(true, billingDay, "Abonelik dogrulandi.");
    }
}
