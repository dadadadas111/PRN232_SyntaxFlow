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

                // Update block properties
                block.Name = request.Name;
                block.Content = request.Content;
                block.UpdatedAt = DateTime.UtcNow;

                // Remove existing tags
                var existingTags = await _context.BlockTags
                    .Where(bt => bt.BlockId == blockId)
                    .ToListAsync();
                _context.BlockTags.RemoveRange(existingTags);

                await _context.SaveChangesAsync();

                // Handle new tags
                await HandleTagsAsync(blockId, request.Tags);

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
    }
}
