using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Block> Blocks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<BlockTag> BlockTags { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Block entity
            builder.Entity<Block>(entity =>
            {
                entity.HasIndex(e => e.OwnerId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsPublic);
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ForkedFrom)
                    .WithMany(e => e.Forks)
                    .HasForeignKey(e => e.ForkedFromId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Tag entity
            builder.Entity<Tag>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure BlockTag entity (many-to-many relationship)
            builder.Entity<BlockTag>(entity =>
            {
                entity.HasIndex(e => new { e.BlockId, e.TagId }).IsUnique();
                
                entity.HasOne(e => e.Block)
                    .WithMany(e => e.BlockTags)
                    .HasForeignKey(e => e.BlockId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Tag)
                    .WithMany(e => e.BlockTags)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
