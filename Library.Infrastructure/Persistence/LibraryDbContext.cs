using Library.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

/// <summary>
/// The main database context for the Library system.
/// Uses SQLite as the database provider.
/// </summary>
public class LibraryDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the DbSet for Resource entities.
    /// </summary>
    public DbSet<ResourceEntity> Resources => Set<ResourceEntity>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the entity mappings for the model.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ResourceEntity>(entity =>
        {
            // Table name
            entity.ToTable("Resources");

            // Primary key
            entity.HasKey(e => e.Id);

            // Type - required, max length 100
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(100);

            // CreatedAt - required
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // UpdatedAt - required
            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // OwnerId - optional, max length 200
            entity.Property(e => e.OwnerId)
                .HasMaxLength(200);

            // MetadataJson - optional, stored as TEXT in SQLite
            entity.Property(e => e.MetadataJson)
                .HasColumnType("TEXT");

            // PayloadJson - required, stored as TEXT in SQLite
            entity.Property(e => e.PayloadJson)
                .IsRequired()
                .HasColumnType("TEXT");

            // SearchText - optional, stored as TEXT in SQLite
            entity.Property(e => e.SearchText)
                .HasColumnType("TEXT");
        });
    }
}
