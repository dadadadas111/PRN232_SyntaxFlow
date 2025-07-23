using System.Threading.Tasks;

namespace Services
{
    public interface IRedisService
    {
        Task SetOtpAsync(string otp, string username, string email, int triesLeft, int ttlSeconds);
        Task<(string Username, string Email, int TriesLeft)?> GetOtpAsync(string otp);
        Task<bool> DecrementOtpTriesAsync(string otp);
        Task DeleteOtpAsync(string otp);
        Task BanUserAsync(string username, int ttlSeconds);
        Task<int?> GetBanTtlAsync(string username);
        Task<bool> IsUserBannedAsync(string username);
    }
}
