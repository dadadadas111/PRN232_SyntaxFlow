using Microsoft.EntityFrameworkCore;
using Models;
using Services.Interface;

namespace Services.Service
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;

        public CommentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsByBlockIdAsync(int blockId)
        {
            var comments = await _context.Comments
                .Where(c => c.BlockId == blockId && !c.IsDeleted)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    BlockId = c.BlockId,
                    UserId = c.UserId,
                    UserName = c.User.UserName ?? "Unknown User",
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return comments;
        }

        public async Task<CommentDto?> CreateCommentAsync(CommentCreateDto dto, string userId)
        {
            // Verify the block exists and is accessible
            var block = await _context.Blocks
                .FirstOrDefaultAsync(b => b.Id == dto.BlockId && 
                    (b.IsPublic || b.OwnerId == userId));

            if (block == null)
                return null;

            var comment = new Comment
            {
                BlockId = dto.BlockId,
                UserId = userId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Return the created comment with user info
            var user = await _context.Users.FindAsync(userId);
            return new CommentDto
            {
                Id = comment.Id,
                BlockId = comment.BlockId,
                UserId = comment.UserId,
                UserName = user?.UserName ?? "Unknown User",
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsOwner = true
            };
        }

        public async Task<CommentDto?> UpdateCommentAsync(int id, string content, string userId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (comment == null)
                return null;

            comment.Content = content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CommentDto
            {
                Id = comment.Id,
                BlockId = comment.BlockId,
                UserId = comment.UserId,
                UserName = comment.User.UserName ?? "Unknown User",
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsOwner = true
            };
        }

        public async Task<bool> DeleteCommentAsync(int id, string userId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (comment == null)
                return false;

            // Soft delete
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCommentCountByBlockIdAsync(int blockId)
        {
            return await _context.Comments
                .CountAsync(c => c.BlockId == blockId && !c.IsDeleted);
        }
    }
}
