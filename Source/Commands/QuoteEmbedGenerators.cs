using System;
using System.Text;
using DSharpPlus.Entities;

namespace Aeoquotes.Commands;

public partial class QuoteCommands
{
    static async Task<DiscordEmbed> QuoteLeaderboard(int leaderboardLength, LeaderboardType leaderboardType)
    {
        DiscordEmbedBuilder embedBuilder = new();
        var quotes = Program.GetQuotes();
        StringBuilder listBuilder = new();
        var topQuotes = leaderboardType switch
        {
            LeaderboardType.Quotee => quotes.CountBy(q => q.userId).OrderByDescending(kvp => kvp.Value).Take(leaderboardLength).ToList(),
            LeaderboardType.Quoter => quotes.CountBy(q => q.quoterId).OrderByDescending(kvp => kvp.Value).Take(leaderboardLength).ToList(),
            LeaderboardType.Weekly_Quoted => quotes.Where(q => (DateTime.Now.Date - q.dateTime.Date).Days <= 7).CountBy(q => q.userId).OrderByDescending(kvp => kvp.Value).Take(leaderboardLength).ToList(),
            _ => []
        };

        string title = leaderboardType switch
        {
            LeaderboardType.Quotee => $"Top {leaderboardLength} Quoted Users",
            LeaderboardType.Quoter => $"Top {leaderboardLength} Quoters",
            LeaderboardType.Weekly_Quoted => $"Top {leaderboardLength} Quoted Users from {DateTime.Now.Subtract(TimeSpan.FromDays(7)).Date:yyyy-MM-dd} to {DateTime.Now.Date:yyyy-MM-dd}",
            _ => "Error lole"
        };
        embedBuilder.WithTitle(title);
        
        for (int i = 0; i < topQuotes.Count; i++)
        {
            listBuilder.Append($"{i + 1}. <@{topQuotes[i].Key}> ({topQuotes[i].Value} quotes)\n");
        }
        embedBuilder.WithFooter($"Total Quotes: {quotes.Count}");
        embedBuilder.WithDescription($"{listBuilder}");
        return embedBuilder.Build();
    }

    static async Task<DiscordEmbed> QuoteEmbed(long id)
    {
        if (id > 0 && id <= Program.maxQuoteId)
        {
            Quote? quote = Program.GetQuotes().Find(q => q.id == id);

            if(quote is null)
            {
                return new DiscordEmbedBuilder()
                {
                    Title = $"#{id} - Error",
                    Description = "**QUOTE IS NULL**"
                };
            }

            DiscordEmbedBuilder embedBuilder = new();
            StringBuilder listBuilder = new();
            embedBuilder.Title = $"#{quote?.id}";
            StringBuilder descBuilder = new();
            descBuilder.Append(quote?.text + "\n");
            descBuilder.Append($"* <@{quote?.userId}> [(Jump)](https://discordapp.com/channels/{quote?.server}/{quote?.channelId}/{quote?.messageId})");
            embedBuilder.Description = descBuilder.ToString();
            embedBuilder.WithTimestamp(quote?.dateTime.ToUniversalTime());
            return embedBuilder.Build();
        }
        else
        {
            return new DiscordEmbedBuilder()
            {
                Title = $"{id} is not in the valid quote id range!"
            }.Build();
        }
    }

    static async Task<DiscordEmbed> QuoteByEmbed(string target)
    {
        List<Quote> quotes = Program.GetQuotes().FindAll(q => q.quoterId == TargetUserIdFromName(target));
        Random rng = new();

        Quote quote = quotes[(int)rng.NextInt64(quotes.Count)];

        if(quote is null)
        {
            return new DiscordEmbedBuilder()
            {
                Title = $"#{target} - Error",
                Description = "**No Quote Found**"
            };
        }

        DiscordEmbedBuilder embedBuilder = new();
        StringBuilder listBuilder = new();
        embedBuilder.Title = $"#{quote?.id}";
        StringBuilder descBuilder = new();
        descBuilder.Append(quote?.text + "\n");
        descBuilder.Append($"* <@{quote?.userId}> [(Jump)](https://discordapp.com/channels/{quote?.server}/{quote?.channelId}/{quote?.messageId})");
        embedBuilder.Description = descBuilder.ToString();
        embedBuilder.WithTimestamp(quote?.dateTime.ToUniversalTime());
        return embedBuilder.Build();
    }

    static async Task<long> UsernameQuote(ulong userId)
    {
        // is this a valid name
        // get all quotes by them
        var quotesByUser = Program.GetQuotes().Where(q => q.userId == userId);
        if (!quotesByUser.Any())
        {
            return -1;
        }
        // they have quotes, so pick a random one
        Random rng = new(DateTime.Now.Microsecond);
        var quoteIndex = rng.NextInt64(quotesByUser.Count());
        // get id of this quote
        // first sort by id
        var sortedQuotes = quotesByUser.OrderBy(q => q.id).ToList();
        // then pull the quote id
        return sortedQuotes[(int)quoteIndex].id;
    }

    static async Task<DiscordEmbed> RandomQuote()
    {
        Random rng = new(DateTime.Now.Microsecond);
        var quotes = Program.GetQuotes();
        var ids = quotes.Select(q => q.id);
        var tries = 0;
        if (quotes.Count > 0)
        {
            long id;
            do {
                id = rng.NextInt64(quotes.Max(q => q.id) + 1);
                tries++;
            } while (!ids.Contains(id));
            Logger.Info($"Random quote selected. Id={id}. {tries} tries needed.");
            return await QuoteEmbed(id);
        }
        return await QuoteEmbed(-1);
    }
}
