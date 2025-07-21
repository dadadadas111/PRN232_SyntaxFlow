using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Block
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty; // JSON content of the Blockly workspace

        [Required]
        public string OwnerId { get; set; } = string.Empty; // User ID who owns this block

        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; } = null!;

        public bool IsPublic { get; set; } = false; // For future sharing functionality

        public int StarCount { get; set; } = 0; // For future star functionality

        public int ForkCount { get; set; } = 0; // For future fork functionality

        public int ViewCount { get; set; } = 0; // Track total views

        public int? ForkedFromId { get; set; } // Reference to original block if this is a fork

        [ForeignKey("ForkedFromId")]
        public Block? ForkedFrom { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<BlockTag> BlockTags { get; set; } = new List<BlockTag>();
        public virtual ICollection<Block> Forks { get; set; } = new List<Block>(); // Blocks forked from this one
        public virtual ICollection<BlockStar> Stars { get; set; } = new List<BlockStar>(); // Users who starred this block
        public virtual ICollection<BlockView> Views { get; set; } = new List<BlockView>(); // Users/anonymous who viewed this block
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>(); // Comments on this block
    }
}
