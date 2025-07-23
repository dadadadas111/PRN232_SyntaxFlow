using Microsoft.AspNetCore.Identity;
using Models;
using System.Threading.Tasks;

namespace Services
{
    public class PasswordResetService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRedisService _redisService;
        public PasswordResetService(UserManager<ApplicationUser> userManager, IRedisService redisService)
        {
            _userManager = userManager;
            _redisService = redisService;
        }
        public async Task<bool> RequestPasswordResetAsync(string email, string otp, int ttlSeconds)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            await _redisService.SetOtpAsync(otp, user.UserName, email, 5, ttlSeconds);
            return true;
        }
        public async Task<bool> ResetPasswordAsync(string otp, string newPassword)
        {
            var otpInfo = await _redisService.GetOtpAsync(otp);
            if (otpInfo == null) return false;
            var user = await _userManager.FindByNameAsync(otpInfo.Value.Username);
            if (user == null) return false;
            var result = await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), newPassword);
            if (result.Succeeded)
            {
                await _redisService.DeleteOtpAsync(otp);
                return true;
            }
            return false;
        }
    }
}
