using Infrastructure;
using Infrastructure.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests;

public class CascadeDeleteTests
{
    [Fact]
    public void CascadeDelete_WhenAuthorIsDeleted_AllAssociatedBooksAreDeleted()
    {
        // Arrange - Create a shared connection for SQLite in-memory
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseSqlite(connection)
            .Options;

        using var dbContext = new BooksDbContext(options);
        dbContext.Database.EnsureCreated();

        // Create an Author
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = "Test Author"
        };
        dbContext.Authors.Add(author);

        // Create multiple Books associated with the Author
        var book1 = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "Test Book 1",
            PublicationYear = 2020,
            AuthorId = author.Id
        };

        var book2 = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-987654-32-1",
            Title = "Test Book 2",
            PublicationYear = 2021,
            AuthorId = author.Id
        };

        dbContext.Books.AddRange(book1, book2);
        dbContext.SaveChanges();

        // Verify books were created
        var booksBeforeDelete = dbContext.Books.Where(b => b.AuthorId == author.Id).ToList();
        Assert.Equal(2, booksBeforeDelete.Count);

        // Act - Delete the Author
        dbContext.Authors.Remove(author);
        dbContext.SaveChanges();

        // Assert - Verify all books were deleted automatically
        var booksAfterDelete = dbContext.Books.Where(b => b.AuthorId == author.Id).ToList();
        Assert.Empty(booksAfterDelete);

        // Verify the author was deleted
        var authorAfterDelete = dbContext.Authors.Find(author.Id);
        Assert.Null(authorAfterDelete);
    }
}

