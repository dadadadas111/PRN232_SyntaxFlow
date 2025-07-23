using Models;
using System.Threading.Tasks;

namespace Services
{
    public class EmailVerificationService
    {
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        public EmailVerificationService(IRedisService redisService, IEmailService emailService)
        {
            _redisService = redisService;
            _emailService = emailService;
        }
        public async Task RequestVerificationAsync(string username, string email, string otp, int ttlSeconds)
        {
            await _redisService.SetOtpAsync(otp, username, email, 5, ttlSeconds);
            var subject = "SyntaxFlow Email Verification";
            var body = $"<p>Your verification code is <b>{otp}</b>. It expires in {ttlSeconds / 60} minutes.</p>";
            await _emailService.SendEmailAsync(email, subject, body);
        }
        public async Task<(bool Success, string? Message, int? BanTtlSeconds)> VerifyOtpAsync(string username, string email, string otp)
        {
            var otpInfo = await _redisService.GetOtpAsync(otp);
            if (otpInfo == null || otpInfo.Value.Username != username || otpInfo.Value.Email != email)
                return (false, "Invalid OTP or user info.", null);
            if (otpInfo.Value.TriesLeft <= 0)
            {
                await _redisService.BanUserAsync(username, 300);
                var banTtl = await _redisService.GetBanTtlAsync(username);
                return (false, "Too many failed attempts. You are banned.", banTtl);
            }
            await _redisService.DecrementOtpTriesAsync(otp);
            await _redisService.DeleteOtpAsync(otp);
            return (true, null, null);
        }
    }
}
