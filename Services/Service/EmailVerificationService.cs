using Models;
using Services.Interface;
using System.Threading.Tasks;

namespace Services.Service
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
            await _redisService.SetOtpAsync(username, otp, email, 5, ttlSeconds);
            var subject = "SyntaxFlow Email Verification";
            var body = $"<p>Your verification code is <b>{otp}</b>. It expires in {ttlSeconds / 60} minutes.</p>";
            await _emailService.SendEmailAsync(email, subject, body);
        }
        public async Task<(bool Success, string? Message, int? BanTtlSeconds)> VerifyOtpAsync(string username, string email, string otp)
        {
            var otpInfo = await _redisService.GetOtpAsync(username);
            if (otpInfo == null || otpInfo.Value.Otp != otp || otpInfo.Value.Email != email)
            {
                // Wrong OTP: decrement tries and ban if needed
                var triesLeft = await _redisService.DecrementOtpTriesAsync(username);
                if (!triesLeft)
                {
                    await _redisService.BanUserAsync(username, 300);
                    var banTtl = await _redisService.GetBanTtlAsync(username);
                    return (false, "Too many failed attempts. You are banned.", banTtl);
                }
                return (false, "Invalid OTP or user info.", null);
            }
            // Correct OTP
            await _redisService.DeleteOtpAsync(username);
            return (true, null, null);
        }
    }
}
