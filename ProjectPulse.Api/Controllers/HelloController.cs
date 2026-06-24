using Microsoft.AspNetCore.Mvc;

namespace ProjectPulse.Api.Controllers;

[ApiController]
[Route("hello")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "Hello from controllers!" });
}
