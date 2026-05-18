using System.Text.Json;

namespace Aeoquotes;

public static class Migrator
{
    public static void OldJsonToEF(string jsonPath, QuotesContext dbContext)
    {
        if (!dbContext.Quotes.Any() && File.Exists(jsonPath))
        {
            dbContext.Database.EnsureDeleted();
            
            bool created = dbContext.Database.EnsureCreated();
            Console.WriteLine($"Database EnsureCreated returned: {created}");

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"Error: Could not find the source JSON file at: {jsonPath}");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var items = JsonSerializer.Deserialize<List<Quote>>(json);

            if (items == null || items.Count == 0)
            {
                Console.WriteLine("Error: JSON was parsed but returned 0 items. Check your JSON structure or class property names.");
                return;
            }

            Console.WriteLine($"Found {items.Count} items in JSON. Adding to database...");

            dbContext.Quotes.AddRange(items);
            
            int rowsWritten = dbContext.SaveChanges();
            Console.WriteLine($"Migration complete! Saved {rowsWritten} rows to the database.");
        }
    }
}