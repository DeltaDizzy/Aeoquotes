using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Aeoquotes;

public class QuoteCommands : BaseCommandModule
{
    #region Command Tasks
    // [Command("help")]
    // public async Task Help(CommandContext ctx)
    // {
    //     StringBuilder descBuilder = new StringBuilder();
    //     DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
    //     {
    //         Title = "AEoTS Quote Bot Help",
    //         Description = "Command listing for the AEoTS quote bot. Note that for all commands, !q and !quote will have the same result."
    //     };
    //     builder.AddField("Leaderboard", "`!quote stats`");
    //     builder.AddField("Getting Quotes", "* Random quote - `!quote`\n* Random quote from a user - `!quote [username]` (Username or display name works!)\n* A specific quote - `!quote [number]`");
    // }

    [Command("q")]
    public async Task Q(CommandContext ctx, [RemainingText] string args)
    {
        Console.WriteLine("q invoked");
        await Quote(ctx, args);
    }

    [Command("quote")]
    public async Task Quote(CommandContext ctx, [RemainingText] string args)
    {
        Console.WriteLine("quote invoked with args:");
        if (args is null)
        {
            args = "";
        }
        var cmdargs = args.Split(" ");
        foreach (var item in cmdargs)
        {
            Console.Write($" {item} ");
        }
        Console.WriteLine();
        // are we asking for a certain quote or a subcommand?
        if (int.TryParse(cmdargs[0], out int id))
        {
            Console.WriteLine("quoting by number");
            DiscordEmbed quote = await QuoteEmbed(id);
            await ctx.Channel.SendMessageAsync(quote);
        } 
        else
        {
            _ = cmdargs[0] switch
            {
                "" => HandleRandom(ctx),
                "stats" => HandleStats(ctx, cmdargs[1]),
                "remove" or "delete" => HandleDelete(ctx, cmdargs[1]),
                "latest" => HandleLatest(ctx),
                string s => HandleUsernameOrInvalid(ctx, s)

            };
        }
    }
#endregion

#region Subcommand Handlers

    private async Task<bool> HandleRandom(CommandContext ctx)
    {
        Console.WriteLine("random quote");
        DiscordEmbed embed = await RandomQuote();
        if (embed.Title is not null)
        {
            await ctx.Channel.SendMessageAsync(embed);
        }
        return true;
    }

    private async Task<bool> HandleStats(CommandContext ctx, string user = "")
    {
        Console.WriteLine("quoting stats");
        DiscordEmbed stats = await QuoteStats();
        await ctx.Channel.SendMessageAsync(stats);
        return true;
    }

    private async Task<bool> HandleDelete(CommandContext ctx, string target)
    {
        if (long.TryParse(target, out long quoteToRemove))
        {
            if (Program.GetQuotes().Count <= quoteToRemove && quoteToRemove > 0)
            {
                Console.WriteLine($"deleting quote {quoteToRemove}");
                // need to remove our reaction
                var message = await ctx.Channel.GetMessageAsync(Program.GetQuotes().Find(q => q.id == quoteToRemove).messageId);
                var user = await ctx.Guild.GetMemberAsync(1503994723118088292);
                // get reaction
                if (message.Reactions.Any(r => r.Emoji.GetDiscordName() == Program.settings.reactName))
                {
                    await message.DeleteReactionAsync(message.Reactions.First(r => r.Emoji.GetDiscordName() == Program.settings.reactName).Emoji, user);
                }
                Program.RemoveQuote(quoteToRemove);
                
                await ctx.Channel.SendMessageAsync($"Quote {quoteToRemove} removed!");
            }
            else
            {
                await ctx.Channel.SendMessageAsync($"Quote {quoteToRemove} not found");
            }
        }
        return true;
    }

    private async Task<bool> HandleLatest(CommandContext ctx)
    {
        DiscordEmbed latestEmbed = await QuoteEmbed(Program.maxQuoteId);
        await ctx.Channel.SendMessageAsync(latestEmbed);
        return true;
    }

    private async Task<bool> HandleUsernameOrInvalid(CommandContext ctx, string arg)
    {
        Console.WriteLine("quoting by username");
        var targetUserId = ctx.Guild.Members.First(m => m.Value.DisplayName.ToLowerInvariant().Equals(arg) || m.Value.Username.ToLowerInvariant().Equals(arg)).Key;
        long quoteId = await UsernameQuote(targetUserId);
        Console.WriteLine(quoteId);
        DiscordEmbed usernameQuote = await QuoteEmbed(quoteId);
        if (usernameQuote.Title is not null)
        {
            await ctx.Channel.SendMessageAsync(usernameQuote);
        }
        return true;
    }
#endregion

#region Embed/Response Generators
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
        if (id > 0 && id <= Program.maxQuoteId)
        {
            Program.Quote quote = Program.GetQuotes().Find(q => q.id == id);
            DiscordEmbedBuilder embedBuilder = new();
            StringBuilder listBuilder = new();
            embedBuilder.Title = $"#{quote.id}";
            StringBuilder descBuilder = new();
            descBuilder.Append(quote.text + "\n");
            descBuilder.Append($"* <@{quote.userId}> [(Jump)](https://discordapp.com/channels/{quote.server}/{quote.channelId}/{quote.messageId})");
            embedBuilder.Description = descBuilder.ToString();
            embedBuilder.WithTimestamp(quote.dateTime.ToUniversalTime());
            return embedBuilder.Build();
        }
        else
        {
            return new DiscordEmbedBuilder()
            {
                Title = "Quote not found!"
            }.Build();
        }
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
        if (quotes.Count > 0)
        {
            long id = rng.NextInt64(quotes.Max(q => q.id) + 1);
            return await QuoteEmbed(id);
        }
        return await QuoteEmbed(-1);
    }
#endregion
}