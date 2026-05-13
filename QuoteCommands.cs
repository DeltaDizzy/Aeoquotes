using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Aeoquotes;

public class QuoteCommands : BaseCommandModule
{
    [Command("q")]
    public async Task Q(CommandContext ctx, [RemainingText] string args)
    {
        await Quote(ctx, args);
    }

    [Command("quote")]
    public async Task Quote(CommandContext ctx, [RemainingText] string args)
    {
        var cmdargs = args.Split(" ");
        // are we asking for a certain quote or a subcommand?
        if (int.TryParse(cmdargs[0], out int id))
        {
            DiscordEmbed quote = await QuoteEmbed(id);
            await ctx.Channel.SendMessageAsync(quote);
        } 
        else
        {
            switch (cmdargs[0])
            {
                case "stats":
                    DiscordEmbed embed = await QuoteStats();
                    await ctx.Channel.SendMessageAsync(embed);
                    break;
                case "remove":
                case "delete":
                    if (int.TryParse(cmdargs[1], out int quoteToRemove))
                    {
                        RemoveQuote(quoteToRemove);
                        await ctx.Channel.SendMessageAsync($"Quote {quoteToRemove} removed!");
                    }
                    break;
                default:
                    // assume arg is a username, so get their id
                    var targetUserId = ctx.Guild.Members.First(m => m.Value.Nickname.ToLowerInvariant().Equals(cmdargs[0]) || m.Value.Username.ToLowerInvariant().Equals(cmdargs[0])).Key;
                    long quoteId = await UsernameQuote(targetUserId);
                    await QuoteEmbed(quoteId);
                    break;
            }  
        }
    }

    void RemoveQuote(int id) => Program.RemoveQuote(id);

    static async Task<DiscordEmbed> QuoteStats()
    {
        // assemble the top 20
        var quotes = Program.GetQuotes();
        var top20 = quotes.CountBy(q => q.userId).OrderByDescending(kvp => kvp.Value).Take(20).ToList();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
        StringBuilder listBuilder = new();
        for (int i = 0; i < top20.Count; i++)
        {
            listBuilder.Append($"{i + 1}. <@{top20[i].Key}> ({top20[i].Value} quotes)\n");
        }
        embedBuilder.WithDescription(listBuilder.ToString());
        return embedBuilder.Build();
    }

    static async Task<DiscordEmbed> QuoteEmbed(long id)
    {
        // assemble the top 20
        Program.Quote quote = Program.GetQuotes().Find(q => q.id == id);
        DiscordEmbedBuilder embedBuilder = new();
        StringBuilder listBuilder = new();
        embedBuilder.Title = $"#{quote.id}";
        StringBuilder descBuilder = new();
        descBuilder.Append(quote.text + "\n");
        descBuilder.Append($"* <@{quote.userId}> [(Jump)](https://discordapp.com/channels/{quote.server}/{quote.channelId}/{quote.messageId})");
        embedBuilder.Description = descBuilder.ToString();
        embedBuilder.WithTimestamp(quote.dateTime);
        return embedBuilder.Build();
    }

    static async Task<long> UsernameQuote(ulong userid)
    {
        // is this a valid name
        // get all quotes by them
        var quotesByUser = Program.GetQuotes().Where(q => q.userId == userid);
        if (!quotesByUser.Any())
        {
            return -1;
        }
        // they have quotes, so pick a random one
        Random rng = new(DateTime.Now.Microsecond);
        var quoteIndex = rng.NextInt64(quotesByUser.Count() + 1);
        // get id of this quote
        // first sort by id
        var sortedQuotes = quotesByUser.OrderBy(q => q.id).ToList();
        // then pull the quote id
        return sortedQuotes[(int)quoteIndex].id;
    }
}