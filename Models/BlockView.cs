using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class BlockView
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BlockId { get; set; }

        [ForeignKey("BlockId")]
        public Block Block { get; set; } = null!;

        public string? UserId { get; set; } 

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string IpAddress { get; set; } = string.Empty; 

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    }
}
