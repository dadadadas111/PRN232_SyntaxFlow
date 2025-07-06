using Microsoft.AspNetCore.Mvc;
using Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var (success, errors) = await _authService.RegisterAsync(request.Username, request.Password);
            if (!success)
                return BadRequest(new { errors });
            var token = await _authService.LoginAsync(request.Username, request.Password);
            if (token == null)
                return Unauthorized( new { message = "Invalid username or password" });
            return Ok(new { token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _authService.LoginAsync(request.Username, request.Password);
            if (token == null)
                return Unauthorized( new { message = "Invalid username or password" });
            return Ok(new { token });
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
