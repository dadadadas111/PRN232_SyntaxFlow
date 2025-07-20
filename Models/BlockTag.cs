using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class BlockTag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BlockId { get; set; }

        [ForeignKey("BlockId")]
        public Block Block { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string TagId { get; set; } = string.Empty;

        [ForeignKey("TagId")]
        public Tag Tag { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
