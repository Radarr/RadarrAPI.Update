using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database.Models;

namespace RadarrAPI.Database
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<UpdateEntity> UpdateEntities { get; set; }

        public DbSet<UpdateFileEntity> UpdateFileEntities { get; set; }

        public DbSet<TraktEntity> TraktEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UpdateEntity>(builder =>
            {
                builder.HasKey(k => k.UpdateEntityId);

                builder.HasIndex(i => new {i.Branch, i.Version}).IsUnique();

                builder.Property(x => x.Version).IsRequired().HasMaxLength(32);
                builder.Property(x => x.ReleaseDate).IsRequired();
                builder.Property(x => x.Branch).IsRequired().HasColumnType("tinyint");
                builder.Property(x => x.NewStr).IsRequired().HasMaxLength(8192);
                builder.Property(x => x.FixedStr).IsRequired().HasMaxLength(8192);

                builder.HasMany(u => u.UpdateFiles)
                    .WithOne(u => u.Update)
                    .HasForeignKey(u => u.UpdateEntityId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
            
            modelBuilder.Entity<UpdateFileEntity>(builder =>
            {
                builder.HasKey(k => new {k.UpdateEntityId, k.OperatingSystem});

                builder.Property(x => x.OperatingSystem).IsRequired().HasColumnType("tinyint");
                builder.Property(x => x.Filename).IsRequired().HasMaxLength(128);
                builder.Property(x => x.Url).IsRequired().HasMaxLength(255);
                builder.Property(x => x.Hash).IsRequired().HasMaxLength(64);
            });

            modelBuilder.Entity<TraktEntity>(builder =>
            {
                builder.HasKey(k => k.Id);
                builder.HasIndex(k => k.State);

                builder.Property(x => x.State).IsRequired();
                builder.Property(x => x.Target).IsRequired().HasMaxLength(255);
                builder.Property(x => x.CreatedAt).IsRequired();
            });
        }
    }
}
