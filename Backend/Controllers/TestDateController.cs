using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/test-date")]
public class TestDateController(AppDateService appDateService) : ControllerBase
{
    [HttpGet]
    public ActionResult<AppDateInfo> Get()
    {
        return appDateService.GetInfo();
    }

    [HttpPost]
    public ActionResult<AppDateInfo> Set(SetTestDateRequest request)
    {
        return appDateService.Set(request.Date);
    }

    [HttpDelete]
    public ActionResult<AppDateInfo> Reset()
    {
        return appDateService.Reset();
    }
}

public record SetTestDateRequest(DateOnly Date);
