using Services.Interface;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.Service
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;
        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SetOtpAsync(string username, string otp, string email, int triesLeft, int ttlSeconds)
        {
            var value = JsonSerializer.Serialize(new { Otp = otp, Email = email, TriesLeft = triesLeft });
            await _db.StringSetAsync($"otp:{username}", value, TimeSpan.FromSeconds(ttlSeconds));
        }

        public async Task<(string Otp, string Email, int TriesLeft)?> GetOtpAsync(string username)
        {
            var value = await _db.StringGetAsync($"otp:{username}");
            if (value.IsNullOrEmpty) return null;
            var obj = JsonSerializer.Deserialize<OtpValue>(value!);
            return obj == null ? null : (obj.Otp, obj.Email, obj.TriesLeft);
        }

        public async Task<bool> DecrementOtpTriesAsync(string username)
        {
            var value = await _db.StringGetAsync($"otp:{username}");
            if (value.IsNullOrEmpty) return false;
            var obj = JsonSerializer.Deserialize<OtpValue>(value!);
            if (obj == null || obj.TriesLeft <= 0) return false;
            obj.TriesLeft--;
            await _db.StringSetAsync($"otp:{username}", JsonSerializer.Serialize(obj));
            return obj.TriesLeft > 0;
        }

        public async Task DeleteOtpAsync(string username)
        {
            await _db.KeyDeleteAsync($"otp:{username}");
        }

        public async Task BanUserAsync(string username, int ttlSeconds)
        {
            await _db.StringSetAsync($"ban:{username}", "1", TimeSpan.FromSeconds(ttlSeconds));
        }

        public async Task<int?> GetBanTtlAsync(string username)
        {
            var ttl = await _db.KeyTimeToLiveAsync($"ban:{username}");
            return ttl?.Seconds;
        }

        public async Task<bool> IsUserBannedAsync(string username)
        {
            return await _db.KeyExistsAsync($"ban:{username}");
        }

        private class OtpValue
        {
            public string Otp { get; set; } = "";
            public string Email { get; set; } = "";
            public int TriesLeft { get; set; }
        }
    }
}