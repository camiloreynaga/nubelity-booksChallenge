using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class BooksDbContext : DbContext
{
    public DbSet<Author> Authors { get; set; }
    public DbSet<Book> Books { get; set; }

    public BooksDbContext(DbContextOptions<BooksDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Author entity
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        // Configure Book entity
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Isbn).IsRequired();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.PublicationYear).IsRequired();
            entity.Property(e => e.AuthorId).IsRequired();

            // Configure unique index on Isbn
            entity.HasIndex(e => e.Isbn).IsUnique();

            // Configure relationship with Author and cascade delete
            entity.HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

