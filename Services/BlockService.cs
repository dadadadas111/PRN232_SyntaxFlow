using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class BlockService : IBlockService
    {
        private readonly ApplicationDbContext _context;

        public BlockService(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<bool> IsBlockStarredByUserAsync(int blockId, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            return await _context.BlockStars
                .AnyAsync(bs => bs.BlockId == blockId && bs.UserId == userId);
        }

        public async Task<IEnumerable<BlockListResponse>> GetUserBlocksAsync(string userId)
        {
            var blocks = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .Where(b => b.OwnerId == userId)
                .OrderByDescending(b => b.UpdatedAt)
                .ToListAsync();

            return blocks.Select(b => new BlockListResponse
            {
                Id = b.Id,
                Name = b.Name,
                OwnerId = b.OwnerId,
                OwnerName = b.Owner.UserName ?? "",
                IsPublic = b.IsPublic,
                StarCount = b.StarCount,
                ForkCount = b.ForkCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray()
            });
        }

        public async Task<BlockResponse?> GetBlockByIdAsync(int blockId, string userId)
        {
            var block = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .FirstOrDefaultAsync(b => b.Id == blockId && b.OwnerId == userId);

            if (block == null)
                return null;

            return new BlockResponse
            {
                Id = block.Id,
                Name = block.Name,
                Content = block.Content,
                OwnerId = block.OwnerId,
                OwnerName = block.Owner.UserName ?? "",
                IsPublic = block.IsPublic,
                StarCount = block.StarCount,
                ForkCount = block.ForkCount,
                ForkedFromId = block.ForkedFromId,
                CreatedAt = block.CreatedAt,
                UpdatedAt = block.UpdatedAt,
                Tags = block.BlockTags.Select(bt => bt.Tag.Name).ToArray()
            };
        }

        public async Task<BlockResponse> CreateBlockAsync(CreateBlockRequest request, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create the block
                var block = new Block
                {
                    Name = request.Name,
                    Content = request.Content,
                    OwnerId = userId,
                    IsPublic = request.IsPublic,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Blocks.Add(block);
                await _context.SaveChangesAsync();

                // Handle tags
                await HandleTagsAsync(block.Id, request.Tags);

                await transaction.CommitAsync();

                // Return the created block
                return await GetBlockByIdAsync(block.Id, userId) ?? throw new InvalidOperationException("Failed to retrieve created block");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BlockResponse?> UpdateBlockAsync(int blockId, UpdateBlockRequest request, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var block = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.Id == blockId && b.OwnerId == userId);

                if (block == null)
                    return null;

                // Update block properties (only if provided)
                if (request.Name != null)
                    block.Name = request.Name;
                
                if (request.Content != null)
                    block.Content = request.Content;
                    
                if (request.IsPublic.HasValue)
                    block.IsPublic = request.IsPublic.Value;
                    
                block.UpdatedAt = DateTime.UtcNow;

                // Handle tags (only if provided)
                if (request.Tags != null)
                {
                    // Remove existing tags
                    var existingTags = await _context.BlockTags
                        .Where(bt => bt.BlockId == blockId)
                        .ToListAsync();
                    _context.BlockTags.RemoveRange(existingTags);
                }

                await _context.SaveChangesAsync();

                // Handle new tags (only if provided)
                if (request.Tags != null)
                {
                    await HandleTagsAsync(blockId, request.Tags);
                }

                await transaction.CommitAsync();

                return await GetBlockByIdAsync(blockId, userId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteBlockAsync(int blockId, string userId)
        {
            var block = await _context.Blocks
                .FirstOrDefaultAsync(b => b.Id == blockId && b.OwnerId == userId);

            if (block == null)
                return false;

            _context.Blocks.Remove(block);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task HandleTagsAsync(int blockId, string[] tagNames)
        {
            if (tagNames == null || tagNames.Length == 0)
                return;

            // Normalize tag names (trim, lowercase for ID)
            var normalizedTags = tagNames
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct()
                .ToArray();

            foreach (var tagName in normalizedTags)
            {
                var tagId = tagName.ToLowerInvariant().Replace(" ", "-");

                // Find or create tag
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);
                if (tag == null)
                {
                    tag = new Tag
                    {
                        Id = tagId,
                        Name = tagName,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                // Create block-tag relationship
                var blockTag = new BlockTag
                {
                    BlockId = blockId,
                    TagId = tag.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BlockTags.Add(blockTag);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<BlockListResponse>> GetPublicBlocksAsync(string? search = null, string[]? tags = null, string sortBy = "created", int page = 1, int size = 10)
        {
            return await GetPublicBlocksAsync(search, tags, sortBy, page, size, null);
        }

        public async Task<IEnumerable<BlockListResponse>> GetPublicBlocksAsync(string? search, string[]? tags, string sortBy, int page, int size, string? currentUserId)
        {
            var query = _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .Where(b => b.IsPublic);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Name.Contains(search) || 
                                        b.BlockTags.Any(bt => bt.Tag.Name.Contains(search)));
            }

            // Apply tag filters
            if (tags != null && tags.Length > 0)
            {
                query = query.Where(b => b.BlockTags.Any(bt => tags.Contains(bt.Tag.Name)));
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "stars" => query.OrderByDescending(b => b.StarCount),
                "forks" => query.OrderByDescending(b => b.ForkCount),
                "updated" => query.OrderByDescending(b => b.UpdatedAt),
                "created" or _ => query.OrderByDescending(b => b.CreatedAt)
            };

            // Apply pagination
            var blocks = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            // Get starred status for current user if authenticated
            var starredBlockIds = new HashSet<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var blockIds = blocks.Select(b => b.Id).ToList();
                starredBlockIds = (await _context.BlockStars
                    .Where(bs => bs.UserId == currentUserId && blockIds.Contains(bs.BlockId))
                    .Select(bs => bs.BlockId)
                    .ToListAsync()).ToHashSet();
            }

            return blocks.Select(b => new BlockListResponse
            {
                Id = b.Id,
                Name = b.Name,
                OwnerId = b.OwnerId,
                OwnerName = b.Owner.UserName ?? "",
                IsPublic = b.IsPublic,
                StarCount = b.StarCount,
                ForkCount = b.ForkCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                IsStarredByCurrentUser = starredBlockIds.Contains(b.Id)
            });
        }

        public async Task<BlockResponse?> GetPublicBlockByIdAsync(int blockId)
        {
            return await GetPublicBlockByIdAsync(blockId, null);
        }

        public async Task<BlockResponse?> GetPublicBlockByIdAsync(int blockId, string? currentUserId)
        {
            var block = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .FirstOrDefaultAsync(b => b.Id == blockId && b.IsPublic);

            if (block == null)
                return null;

            bool isStarred = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                isStarred = await IsBlockStarredByUserAsync(blockId, currentUserId);
            }

            return new BlockResponse
            {
                Id = block.Id,
                Name = block.Name,
                Content = block.Content,
                OwnerId = block.OwnerId,
                OwnerName = block.Owner.UserName ?? "",
                IsPublic = block.IsPublic,
                StarCount = block.StarCount,
                ForkCount = block.ForkCount,
                ForkedFromId = block.ForkedFromId,
                CreatedAt = block.CreatedAt,
                UpdatedAt = block.UpdatedAt,
                Tags = block.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                IsStarredByCurrentUser = isStarred
            };
        }

        public async Task<StarResponse> StarBlockAsync(int blockId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if block exists and is accessible for starring
                var block = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.Id == blockId && (b.IsPublic || b.OwnerId == userId));

                if (block == null)
                    throw new InvalidOperationException("Block not found or not accessible for starring");

                // Users cannot star their own blocks
                if (block.OwnerId == userId)
                    throw new InvalidOperationException("Users cannot star their own blocks");

                // Check if already starred
                var existingStar = await _context.BlockStars
                    .FirstOrDefaultAsync(bs => bs.BlockId == blockId && bs.UserId == userId);

                if (existingStar != null)
                {
                    // Already starred, return current state
                    return new StarResponse
                    {
                        IsStarred = true,
                        StarCount = block.StarCount,
                        StarredAt = existingStar.CreatedAt
                    };
                }

                // Create new star
                var blockStar = new BlockStar
                {
                    BlockId = blockId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BlockStars.Add(blockStar);

                // Increment star count
                block.StarCount++;
                block.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new StarResponse
                {
                    IsStarred = true,
                    StarCount = block.StarCount,
                    StarredAt = blockStar.CreatedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StarResponse> UnstarBlockAsync(int blockId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if block exists
                var block = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.Id == blockId);

                if (block == null)
                    throw new InvalidOperationException("Block not found");

                // Find the star
                var blockStar = await _context.BlockStars
                    .FirstOrDefaultAsync(bs => bs.BlockId == blockId && bs.UserId == userId);

                if (blockStar == null)
                {
                    // Not starred, return current state
                    return new StarResponse
                    {
                        IsStarred = false,
                        StarCount = block.StarCount,
                        StarredAt = null
                    };
                }

                // Remove star
                _context.BlockStars.Remove(blockStar);

                // Decrement star count
                block.StarCount = Math.Max(0, block.StarCount - 1);
                block.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new StarResponse
                {
                    IsStarred = false,
                    StarCount = block.StarCount,
                    StarredAt = null
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<BlockListResponse>> GetStarredBlocksAsync(string userId)
        {
            var starredBlocks = await _context.BlockStars
                .Include(bs => bs.Block)
                .ThenInclude(b => b.Owner)
                .Include(bs => bs.Block)
                .ThenInclude(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .Where(bs => bs.UserId == userId)
                .OrderByDescending(bs => bs.CreatedAt)
                .Select(bs => bs.Block)
                .ToListAsync();

            return starredBlocks.Select(b => new BlockListResponse
            {
                Id = b.Id,
                Name = b.Name,
                OwnerId = b.OwnerId,
                OwnerName = b.Owner.UserName ?? "",
                IsPublic = b.IsPublic,
                StarCount = b.StarCount,
                ForkCount = b.ForkCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                IsStarredByCurrentUser = true // Always true for starred blocks list
            });
        }

        public async Task<IEnumerable<StarUserResponse>> GetBlockStarsAsync(int blockId)
        {
            // Check if block exists and is public
            var block = await _context.Blocks
                .FirstOrDefaultAsync(b => b.Id == blockId && b.IsPublic);

            if (block == null)
                throw new InvalidOperationException("Block not found or not public");

            var stars = await _context.BlockStars
                .Include(bs => bs.User)
                .Where(bs => bs.BlockId == blockId)
                .OrderByDescending(bs => bs.CreatedAt)
                .ToListAsync();

            return stars.Select(bs => new StarUserResponse
            {
                UserId = bs.UserId,
                UserName = bs.User.UserName ?? "",
                StarredAt = bs.CreatedAt
            });
        }
    }
}
