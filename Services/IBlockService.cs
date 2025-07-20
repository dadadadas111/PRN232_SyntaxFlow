using Models;

namespace Services
{
    public interface IBlockService
    {
        Task<IEnumerable<BlockListResponse>> GetUserBlocksAsync(string userId);
        Task<BlockResponse?> GetBlockByIdAsync(int blockId, string userId);
        Task<BlockResponse> CreateBlockAsync(CreateBlockRequest request, string userId);
        Task<BlockResponse?> UpdateBlockAsync(int blockId, UpdateBlockRequest request, string userId);
        Task<bool> DeleteBlockAsync(int blockId, string userId);
        
        // Public block discovery methods
        Task<IEnumerable<BlockListResponse>> GetPublicBlocksAsync(string? search = null, string[]? tags = null, string sortBy = "created", int page = 1, int size = 10);
        Task<IEnumerable<BlockListResponse>> GetPublicBlocksAsync(string? search, string[]? tags, string sortBy, int page, int size, string? currentUserId);
        Task<BlockResponse?> GetPublicBlockByIdAsync(int blockId);
        Task<BlockResponse?> GetPublicBlockByIdAsync(int blockId, string? currentUserId);
        
        // Star system methods
        Task<StarResponse> StarBlockAsync(int blockId, string userId);
        Task<StarResponse> UnstarBlockAsync(int blockId, string userId);
        Task<IEnumerable<BlockListResponse>> GetStarredBlocksAsync(string userId);
        Task<IEnumerable<StarUserResponse>> GetBlockStarsAsync(int blockId);
    }
}
