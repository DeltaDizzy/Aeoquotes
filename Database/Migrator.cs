using System.Text.Json;

namespace Aeoquotes;

public static class Migrator
{
    public static void OldJsonToEF(string jsonpath, QuotesContext dbContext)
    {
        // make sure ef is up
        dbContext.Database.EnsureCreated();

        // if ef store is empty, migrate quotes
        if (!dbContext.Quotes.Any() && File.Exists(jsonpath))
        {
            string oldQuotesJson = File.ReadAllText(jsonpath);
            var oldQuotes = JsonSerializer.Deserialize<List<Quote>>(oldQuotesJson);

            if (oldQuotes is not null)
            {
                dbContext.Quotes.AddRange(oldQuotes);
            }
        }

        dbContext.SaveChanges();
    }
}