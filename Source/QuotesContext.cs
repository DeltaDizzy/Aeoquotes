using Microsoft.EntityFrameworkCore;

namespace Aeoquotes;

public class QuotesContext : DbContext
{
    public DbSet<Quote> Quotes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // We are using JSON for now
        optionsBuilder.UseSqlite(@$"Data Source={Program.GetProjectRoot()}/DB/quotes.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>().HasKey(q => q.id);
    }
}