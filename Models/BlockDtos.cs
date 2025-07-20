using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class CreateBlockRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty; // JSON content of the Blockly workspace

        public string[] Tags { get; set; } = Array.Empty<string>();
        
        public bool IsPublic { get; set; } = false;
    }

    public class UpdateBlockRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        public string? Content { get; set; }

        public string[]? Tags { get; set; }
        
        public bool? IsPublic { get; set; }
    }

    public class BlockResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public int StarCount { get; set; }
        public int ForkCount { get; set; }
        public int? ForkedFromId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class BlockListResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public int StarCount { get; set; }
        public int ForkCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
