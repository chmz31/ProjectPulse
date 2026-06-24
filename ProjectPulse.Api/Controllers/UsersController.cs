using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectPulse.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        // GET /users/me
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var name = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                Id = id,
                Email = email,
                DisplayName = name,
                Role = role
            });
        }
    }
}
