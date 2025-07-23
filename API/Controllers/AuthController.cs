using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly EmailVerificationService _emailVerificationService;
        private readonly PasswordResetService _passwordResetService;

        public AuthController(
            IAuthService authService,
            IRedisService redisService,
            IEmailService emailService,
            EmailVerificationService emailVerificationService,
            PasswordResetService passwordResetService)
        {
            _authService = authService;
            _redisService = redisService;
            _emailService = emailService;
            _emailVerificationService = emailVerificationService;
            _passwordResetService = passwordResetService;
        }

        private string GetCurrentUserName() => User?.Identity?.Name ?? "";

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var (success, errors) = await _authService.RegisterAsync(request.Username, request.Password);
            if (!success)
                return BadRequest(new { errors });
            var token = await _authService.LoginAsync(request.Username, request.Password);
            if (token == null)
                return Unauthorized(new { message = "Invalid username or password" });
            var emailVerified = await _authService.IsEmailVerifiedAsync(request.Username);
            return Ok(new { token, email_verified = emailVerified });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _authService.LoginAsync(request.Username, request.Password);
            if (token == null)
                return Unauthorized(new { message = "Invalid username or password" });
            var emailVerified = await _authService.IsEmailVerifiedAsync(request.Username);
            return Ok(new { token, email_verified = emailVerified });
        }

        [Authorize]
        [HttpPost("request-email-verification")]
        public async Task<IActionResult> RequestEmailVerification([FromBody] RequestEmailVerificationDto dto)
        {
            var username = GetCurrentUserName();
            if (await _redisService.IsUserBannedAsync(username))
            {
                var ttl = await _redisService.GetBanTtlAsync(username);
                return StatusCode(429, new { message = "Banned", ban_ttl = ttl });
            }
            if (!dto.Email.Contains("@"))
                return BadRequest(new { message = "Invalid email format" });
            var emailExists = await _authService.IsEmailVerifiedAsync(username);
            if (emailExists)
                return BadRequest(new { message = "Email already exists for this user" });
            var otp = OtpService.GenerateOtp();
            await _emailVerificationService.RequestVerificationAsync(username, dto.Email, otp, 300);
            return Ok(new { success = true });
        }

        [Authorize]
        [HttpPost("verify-email-otp")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyOtpDto dto)
        {
            var username = GetCurrentUserName();
            var otpInfo = await _redisService.GetOtpAsync(dto.Otp);
            if (otpInfo == null)
                return BadRequest(new { message = "Invalid OTP" });
            var email = otpInfo.Value.Email;
            var result = await _emailVerificationService.VerifyOtpAsync(username, email, dto.Otp);
            if (!result.Success)
                return StatusCode(result.BanTtlSeconds.HasValue ? 429 : 400, new { message = result.Message, ban_ttl = result.BanTtlSeconds });
            var updated = await _authService.SetEmailVerifiedAsync(username, email);
            if (!updated)
                return BadRequest(new { message = "Failed to update email" });
            return Ok(new { success = true });
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
        {
            var otp = OtpService.GenerateOtp();
            var success = await _passwordResetService.RequestPasswordResetAsync(dto.Email, otp, 300);
            if (!success)
                return BadRequest(new { message = "Email not found" });
            await _emailService.SendEmailAsync(dto.Email, "SyntaxFlow Password Reset", $"<p>Your password reset code is <b>{otp}</b>. It expires in 5 minutes.</p>");
            return Ok(new { success = true });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var success = await _passwordResetService.ResetPasswordAsync(dto.Otp, dto.NewPassword);
            if (!success)
                return BadRequest(new { message = "Invalid OTP or password" });
            return Ok(new { success = true });
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
