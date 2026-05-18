using Microsoft.EntityFrameworkCore;

namespace Aeoquotes;

public class QuotesContext : DbContext
{
    public DbSet<Quote> Quotes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(@$"Data Source={Program.GetProjectRoot()}/Database/quotes.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>().HasKey(q => q.id);
    }
}