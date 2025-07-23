using System.Threading.Tasks;

namespace Services
{
    public interface IRedisService
    {
        Task SetOtpAsync(string username, string otp, string email, int triesLeft, int ttlSeconds);
        Task<(string Otp, string Email, int TriesLeft)?> GetOtpAsync(string username);
        Task<bool> DecrementOtpTriesAsync(string username);
        Task DeleteOtpAsync(string username);
        Task BanUserAsync(string username, int ttlSeconds);
        Task<int?> GetBanTtlAsync(string username);
        Task<bool> IsUserBannedAsync(string username);
    }
}
