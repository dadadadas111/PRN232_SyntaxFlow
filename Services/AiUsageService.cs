using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Services
{
    public class AiUsageService : IAiUsageService
    {
        private readonly IDatabase _database;
        private readonly ILogger<AiUsageService> _logger;
        private const int DAILY_AI_LIMIT = 5;
        private const string AI_USAGE_KEY_PREFIX = "ai_usage:";

        public AiUsageService(IConnectionMultiplexer redis, ILogger<AiUsageService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> TryConsumeAiUsageAsync(string userId)
        {
            var key = GetUsageKey(userId);
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            
            try
            {
                // Get current usage for today
                var currentUsageKey = $"{key}:{today}";
                var currentUsage = await _database.StringGetAsync(currentUsageKey);
                var usage = currentUsage.HasValue ? (int)currentUsage : 0;
                
                // Check if limit exceeded
                if (usage >= DAILY_AI_LIMIT)
                {
                    _logger.LogWarning("AI usage limit exceeded for user {UserId}. Current: {Usage}, Limit: {Limit}", 
                        userId, usage, DAILY_AI_LIMIT);
                    return false;
                }
                
                // Increment usage
                await _database.StringIncrementAsync(currentUsageKey);
                
                // Set expiry for tomorrow (only if key was just created)
                if (usage == 0)
                {
                    var tomorrow = DateTime.UtcNow.Date.AddDays(1);
                    var secondsUntilTomorrow = (int)(tomorrow - DateTime.UtcNow).TotalSeconds;
                    await _database.KeyExpireAsync(currentUsageKey, TimeSpan.FromSeconds(secondsUntilTomorrow));
                }
                
                var newUsage = usage + 1;
                _logger.LogInformation("AI usage incremented for user {UserId}. New count: {Count}", userId, newUsage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking AI usage for user {UserId}", userId);
                // On error, allow usage (fail open)
                return true;
            }
        }

        public async Task<AiUsageInfo> GetAiUsageAsync(string userId)
        {
            var key = GetUsageKey(userId);
            var today = DateTime.UtcNow;
            var todayStr = today.ToString("yyyy-MM-dd");
            
            try
            {
                var currentUsageKey = $"{key}:{todayStr}";
                var currentUsage = await _database.StringGetAsync(currentUsageKey);
                var usage = currentUsage.HasValue ? (int)currentUsage : 0;
                
                return new AiUsageInfo
                {
                    CurrentUsage = usage,
                    DailyLimit = DAILY_AI_LIMIT,
                    ResetTime = today.Date.AddDays(1) // Next day at midnight UTC
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI usage for user {UserId}", userId);
                // On error, return safe default
                return new AiUsageInfo
                {
                    CurrentUsage = 0,
                    DailyLimit = DAILY_AI_LIMIT,
                    ResetTime = today.Date.AddDays(1)
                };
            }
        }

        public async Task<bool> CanUseAiAsync(string userId)
        {
            var usageInfo = await GetAiUsageAsync(userId);
            return usageInfo.CanUse;
        }

        private string GetUsageKey(string userId) => $"{AI_USAGE_KEY_PREFIX}{userId}";
    }
}
