using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Tag
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty; // Using readable tag name as ID

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<BlockTag> BlockTags { get; set; } = new List<BlockTag>();
    }
}
