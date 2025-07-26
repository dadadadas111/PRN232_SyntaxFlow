using Microsoft.EntityFrameworkCore;
using Models;
using Services.Interface;

namespace Services.Service
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
                .Include(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
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
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                ForkedFromId = b.ForkedFromId,
                ForkedFromName = b.ForkedFrom?.Name,
                ForkedFromOwnerName = b.ForkedFrom?.Owner?.UserName
            });
        }

        public async Task<BlockResponse?> GetBlockByIdAsync(int blockId, string userId)
        {
            var block = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
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
                ViewCount = block.ViewCount,
                ForkedFromId = block.ForkedFromId,
                ForkedFromName = block.ForkedFrom?.Name,
                ForkedFromOwnerName = block.ForkedFrom?.Owner?.UserName,
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var block = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.Id == blockId && b.OwnerId == userId);

                if (block == null)
                    return false;

                // Handle forked blocks: set ForkedFromId to null so they become "orphaned"
                var forkedBlocks = await _context.Blocks
                    .Where(b => b.ForkedFromId == blockId)
                    .ToListAsync();

                foreach (var forkedBlock in forkedBlocks)
                {
                    forkedBlock.ForkedFromId = null;
                    forkedBlock.UpdatedAt = DateTime.UtcNow;
                }

                // Delete the original block
                _context.Blocks.Remove(block);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
                .Include(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
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
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                IsStarredByCurrentUser = starredBlockIds.Contains(b.Id),
                ForkedFromId = b.ForkedFromId,
                ForkedFromName = b.ForkedFrom?.Name,
                ForkedFromOwnerName = b.ForkedFrom?.Owner?.UserName
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
                .Include(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
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
                ViewCount = block.ViewCount,
                ForkedFromId = block.ForkedFromId,
                ForkedFromName = block.ForkedFrom?.Name,
                ForkedFromOwnerName = block.ForkedFrom?.Owner?.UserName,
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
                .ThenInclude(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
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
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                IsStarredByCurrentUser = true, // Always true for starred blocks list
                ForkedFromId = b.ForkedFromId,
                ForkedFromName = b.ForkedFrom?.Name,
                ForkedFromOwnerName = b.ForkedFrom?.Owner?.UserName
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

        // Fork system methods
        public async Task<ForkResponse> ForkBlockAsync(int blockId, string userId, string? customName = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the original block
                var originalBlock = await _context.Blocks
                    .Include(b => b.BlockTags)
                    .ThenInclude(bt => bt.Tag)
                    .FirstOrDefaultAsync(b => b.Id == blockId);

                if (originalBlock == null)
                    throw new InvalidOperationException("Block not found");

                // Validate: users can fork public blocks and their own blocks
                if (!originalBlock.IsPublic && originalBlock.OwnerId != userId)
                    throw new UnauthorizedAccessException("You can only fork public blocks or your own blocks");

                // Create the fork name
                var forkName = !string.IsNullOrWhiteSpace(customName) 
                    ? customName.Trim() 
                    : $"Fork of {originalBlock.Name}";

                // Create the forked block
                var forkedBlock = new Block
                {
                    Name = forkName,
                    Content = originalBlock.Content, // Copy the content
                    OwnerId = userId,
                    IsPublic = false, // Forks start as private
                    StarCount = 0, // Reset counts
                    ForkCount = 0,
                    ForkedFromId = blockId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Blocks.Add(forkedBlock);
                await _context.SaveChangesAsync();

                // Copy tags from original block
                foreach (var blockTag in originalBlock.BlockTags)
                {
                    var newBlockTag = new BlockTag
                    {
                        BlockId = forkedBlock.Id,
                        TagId = blockTag.TagId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.BlockTags.Add(newBlockTag);
                }

                // Increment original block's fork count
                originalBlock.ForkCount++;
                originalBlock.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ForkResponse
                {
                    ForkedBlockId = forkedBlock.Id,
                    ForkedBlockName = forkedBlock.Name,
                    OriginalForkCount = originalBlock.ForkCount,
                    ForkedAt = forkedBlock.CreatedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<BlockListResponse>> GetBlockForksAsync(int blockId)
        {
            // Check if the original block exists and is public (or user owns it)
            var originalBlock = await _context.Blocks
                .FirstOrDefaultAsync(b => b.Id == blockId);

            if (originalBlock == null)
                throw new InvalidOperationException("Block not found");

            var forks = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .Where(b => b.ForkedFromId == blockId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return forks.Select(b => new BlockListResponse
            {
                Id = b.Id,
                Name = b.Name,
                OwnerId = b.OwnerId,
                OwnerName = b.Owner.UserName ?? "",
                IsPublic = b.IsPublic,
                StarCount = b.StarCount,
                ForkCount = b.ForkCount,
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray()
            });
        }

        public async Task<IEnumerable<BlockListResponse>> GetForkedBlocksAsync(string userId)
        {
            var forkedBlocks = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .Where(b => b.OwnerId == userId && b.ForkedFromId != null)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return forkedBlocks.Select(b => new BlockListResponse
            {
                Id = b.Id,
                Name = b.Name,
                OwnerId = b.OwnerId,
                OwnerName = b.Owner.UserName ?? "",
                IsPublic = b.IsPublic,
                StarCount = b.StarCount,
                ForkCount = b.ForkCount,
                ViewCount = b.ViewCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Tags = b.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                ForkedFromId = b.ForkedFromId,
                ForkedFromName = b.ForkedFrom?.Name,
                ForkedFromOwnerName = b.ForkedFrom?.Owner?.UserName
            });
        }

        // Enhanced block management methods
        public async Task<IEnumerable<BlockListResponse>> GetTrendingBlocksAsync(int days = 7, int page = 1, int size = 10)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            
            // Get public blocks with recent activity
            var trendingBlocks = await _context.Blocks
                .Include(b => b.Owner)
                .Include(b => b.ForkedFrom)
                .ThenInclude(f => f!.Owner)
                .Include(b => b.BlockTags)
                .ThenInclude(bt => bt.Tag)
                .Where(b => b.IsPublic)
                .Select(b => new
                {
                    Block = b,
                    // Calculate trending score: recent stars + recent forks + recent views, weighted by recency
                    RecentStars = b.Stars.Count(s => s.CreatedAt >= cutoffDate),
                    RecentForks = b.Forks.Count(f => f.CreatedAt >= cutoffDate),
                    RecentViews = b.Views.Count(v => v.ViewedAt >= cutoffDate),
                    TotalActivity = b.StarCount + b.ForkCount + b.ViewCount / 10, // Weight views less heavily
                    DaysSinceUpdate = EF.Functions.DateDiffDay(b.UpdatedAt, DateTime.UtcNow)
                })
                .OrderByDescending(x => 
                    (x.RecentStars * 3 + x.RecentForks * 2 + x.RecentViews * 0.1) / Math.Max(1, x.DaysSinceUpdate + 1) + 
                    x.TotalActivity * 0.1) // Boost for overall popularity
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return trendingBlocks.Select(x => new BlockListResponse
            {
                Id = x.Block.Id,
                Name = x.Block.Name,
                OwnerId = x.Block.OwnerId,
                OwnerName = x.Block.Owner.UserName ?? "",
                IsPublic = x.Block.IsPublic,
                StarCount = x.Block.StarCount,
                ForkCount = x.Block.ForkCount,
                ViewCount = x.Block.ViewCount,
                CreatedAt = x.Block.CreatedAt,
                UpdatedAt = x.Block.UpdatedAt,
                Tags = x.Block.BlockTags.Select(bt => bt.Tag.Name).ToArray(),
                ForkedFromId = x.Block.ForkedFromId,
                ForkedFromName = x.Block.ForkedFrom?.Name,
                ForkedFromOwnerName = x.Block.ForkedFrom?.Owner?.UserName
            });
        }

        public async Task TrackBlockViewAsync(int blockId, string? userId = null, string? ipAddress = null)
        {
            // Check if block exists and is public (or user owns it)
            var block = await _context.Blocks
                .FirstOrDefaultAsync(b => b.Id == blockId && (b.IsPublic || b.OwnerId == userId));

            if (block == null)
                return; // Don't track views for non-existent or private blocks

            // Use provided IP address or default
            ipAddress ??= "127.0.0.1";

            // Check if this user/IP has viewed this block recently (within last hour to prevent spam)
            var recentView = await _context.BlockViews
                .AnyAsync(v => v.BlockId == blockId && 
                    (userId != null && v.UserId == userId || 
                     userId == null && v.IpAddress == ipAddress) &&
                    v.ViewedAt >= DateTime.UtcNow.AddHours(-1));

            if (recentView)
                return; // Don't count duplicate views within an hour

            // Create new view record
            var blockView = new BlockView
            {
                BlockId = blockId,
                UserId = userId,
                IpAddress = ipAddress,
                ViewedAt = DateTime.UtcNow
            };

            _context.BlockViews.Add(blockView);

            // Increment view count on block
            block.ViewCount++;
            block.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
