namespace Services.Interface
{
    public interface IAiUsageService
    {
        /// <summary>
        /// Check if user can use AI feature and increment usage if allowed
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if allowed, false if limit exceeded</returns>
        Task<bool> TryConsumeAiUsageAsync(string userId);
        
        /// <summary>
        /// Get current AI usage for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Usage info with current count and limit</returns>
        Task<AiUsageInfo> GetAiUsageAsync(string userId);
        
        /// <summary>
        /// Check if user can use AI feature without consuming usage
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if allowed, false if limit exceeded</returns>
        Task<bool> CanUseAiAsync(string userId);
    }

    public class AiUsageInfo
    {
        public int CurrentUsage { get; set; }
        public int DailyLimit { get; set; }
        public int RemainingUsage => Math.Max(0, DailyLimit - CurrentUsage);
        public DateTime ResetTime { get; set; }
        public bool CanUse => CurrentUsage < DailyLimit;
    }
}
