using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;
        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SetOtpAsync(string otp, string username, string email, int triesLeft, int ttlSeconds)
        {
            var value = JsonSerializer.Serialize(new { Username = username, Email = email, TriesLeft = triesLeft });
            await _db.StringSetAsync($"otp:{otp}", value, TimeSpan.FromSeconds(ttlSeconds));
        }

        public async Task<(string Username, string Email, int TriesLeft)?> GetOtpAsync(string otp)
        {
            var value = await _db.StringGetAsync($"otp:{otp}");
            if (value.IsNullOrEmpty) return null;
            var obj = JsonSerializer.Deserialize<OtpValue>(value!);
            return obj == null ? null : (obj.Username, obj.Email, obj.TriesLeft);
        }

        public async Task<bool> DecrementOtpTriesAsync(string otp)
        {
            var value = await _db.StringGetAsync($"otp:{otp}");
            if (value.IsNullOrEmpty) return false;
            var obj = JsonSerializer.Deserialize<OtpValue>(value!);
            if (obj == null || obj.TriesLeft <= 0) return false;
            obj.TriesLeft--;
            await _db.StringSetAsync($"otp:{otp}", JsonSerializer.Serialize(obj));
            return obj.TriesLeft > 0;
        }

        public async Task DeleteOtpAsync(string otp)
        {
            await _db.KeyDeleteAsync($"otp:{otp}");
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
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public int TriesLeft { get; set; }
        }
    }
}