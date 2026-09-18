using Microsoft.EntityFrameworkCore;

namespace Aeoquotes;

public class QuotesContext : DbContext
{
    public DbSet<Quote> Quotes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Join(Program.GetProjectRoot(), "Database", "quotes.db");

        optionsBuilder.UseSqlite(@$"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>().HasKey(q => q.id);
    }
}