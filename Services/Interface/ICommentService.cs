using Models;

namespace Services.Interface
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentDto>> GetCommentsByBlockIdAsync(int blockId);
        Task<CommentDto?> CreateCommentAsync(CommentCreateDto dto, string userId);
        Task<CommentDto?> UpdateCommentAsync(int id, string content, string userId);
        Task<bool> DeleteCommentAsync(int id, string userId);
        Task<int> GetCommentCountByBlockIdAsync(int blockId);
    }
}
