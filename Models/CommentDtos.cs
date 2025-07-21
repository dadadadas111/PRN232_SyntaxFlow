using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class CommentDto
    {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsOwner { get; set; } = false; // True if current user owns this comment
    }

    public class CommentCreateDto
    {
        [Required]
        public int BlockId { get; set; }

        [Required]
        [MaxLength(1000)]
        [MinLength(1)]
        public string Content { get; set; } = string.Empty;
    }

    public class CommentUpdateDto
    {
        [Required]
        [MaxLength(1000)]
        [MinLength(1)]
        public string Content { get; set; } = string.Empty;
    }

    public class CommentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CommentDto? Comment { get; set; }
    }
}
