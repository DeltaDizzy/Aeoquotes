using System.Text.Json;

namespace Aeoquotes;

public static class Migrator
{
    public static void OldJsonToEF(string jsonPath, QuotesContext dbContext)
    {
        if (!dbContext.Quotes.Any() && File.Exists(jsonPath))
        {
            // 1. Completely wipe any existing database file at that path to ensure a fresh slate
        dbContext.Database.EnsureDeleted();
        
        // 2. This WILL return true now, creating a brand new file with the correct tables
        bool created = dbContext.Database.EnsureCreated();
        Console.WriteLine($"[Diagnostic] Database EnsureCreated returned: {created}");

        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"[Error] Could not find the source JSON file at: {jsonPath}");
            return;
        }

        string json = File.ReadAllText(jsonPath);
        var items = JsonSerializer.Deserialize<List<Quote>>(json);

        if (items == null || items.Count == 0)
        {
            Console.WriteLine("[Error] JSON was parsed but returned 0 items. Check your JSON structure or class property names.");
            return;
        }

        Console.WriteLine($"[Diagnostic] Found {items.Count} items in JSON. Attaching to EF Core...");

        // 3. Add to the database
        dbContext.Quotes.AddRange(items);
        
        // 4. Force the save and capture how many rows were actually written
        int rowsWritten = dbContext.SaveChanges();
        Console.WriteLine($"[Success] Migration complete! Saved {rowsWritten} rows directly to the SQLite file.");
        }
    }
}