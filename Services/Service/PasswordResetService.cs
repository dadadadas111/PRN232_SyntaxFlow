using Microsoft.AspNetCore.Identity;
using Models;
using Services.Interface;
using System.Threading.Tasks;

namespace Services.Service
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
            await _redisService.SetOtpAsync(user.UserName, otp, email, 5, ttlSeconds);
            return true;
        }
        public async Task<bool> ResetPasswordAsync(string username, string otp, string newPassword)
        {
            var otpInfo = await _redisService.GetOtpAsync(username);
            if (otpInfo == null || otpInfo.Value.Otp != otp) return false;
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;
            var result = await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), newPassword);
            if (result.Succeeded)
            {
                await _redisService.DeleteOtpAsync(username);
                return true;
            }
            return false;
        }
    }
}
