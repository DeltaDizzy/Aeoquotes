using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using log4net;

namespace Aeoquotes;

public class QuoteCommands : BaseCommandModule
{
    private static readonly ulong AEOTS_SERVER_ID = 1503994723118088292;
    private static readonly ILog Logger = LogManager.GetLogger(typeof(QuoteCommands));
    #region Command Tasks

    [Command("q")]
    public async Task Q(CommandContext ctx, [RemainingText] string args)
    {
        Logger.Debug("q invoked");
        await Quote(ctx, args);
    }

    [Command("quote")]
    public async Task Quote(CommandContext ctx, [RemainingText] string args)
    {
        StringBuilder argsB = new();
        args ??= "";
        var cmdargs = args.Split(" ");
        foreach (var item in cmdargs)
        {
            argsB.Append($" {item} ");
        }
        Logger.Info($"Command `quote` invoked with args: {argsB}");
        // are we asking for a certain quote or a subcommand?
        if (int.TryParse(cmdargs[0], out int id))
        {
            Logger.Info($"quoting by number: {id}");
            DiscordEmbed quote = await QuoteEmbed(id);
            await ctx.Channel.SendMessageAsync(quote);
        } 
        else
        {
            _ = cmdargs[0] switch
            {
                "" => HandleRandom(ctx),
                "stats" => HandleStats(ctx, cmdargs.Length >= 2 ? cmdargs[1] : null),
                "remove" or "delete" => HandleDelete(ctx, cmdargs[1]),
                "latest" => HandleLatest(ctx),
                string s => HandleUsernameOrInvalid(ctx, args)

            };
        }
    }
#endregion

#region Subcommand Handlers

    private async Task<bool> HandleRandom(CommandContext ctx)
    {
        DiscordEmbed embed = await RandomQuote();
        if (embed.Title is not null)
        {
            await ctx.Channel.SendMessageAsync(embed);
        }
        return true;
    }

    private async Task<bool> HandleStats(CommandContext ctx, string? data)
    {
        DiscordEmbed stats;
        int? leaderboardLength = int.TryParse(data, out int num) ? num : null;
        // no number after, user stats or base leaderboard
        if (leaderboardLength is null)
        {
            Logger.Info($"Generating leaderboard, length=25");
            stats = await QuoteLeaderboard(leaderboardLength);
        } 
        else
        {
            Logger.Info($"Generating leaderboard, length={leaderboardLength}");
            stats = await QuoteLeaderboard(leaderboardLength.Value);
        }

        await ctx.Channel.SendMessageAsync(stats);
        return true;
    }

    private async Task<bool> HandleDelete(CommandContext ctx, string target)
    {
        if (long.TryParse(target, out long quoteToRemove))
        {
            if (Program.GetQuotes().Count <= quoteToRemove && quoteToRemove > 0)
            {
                Logger.Info($"deleting quote {quoteToRemove}");
                // need to remove our reaction
                Quote? quote = Program.GetQuotes().Find(q => q.id == quoteToRemove);
                if (quote is null)
                {
                    await ctx.Channel.SendMessageAsync("Quote not found");
                    return false;
                }
                var message = await ctx.Channel.GetMessageAsync(quote.messageId);
                var user = await ctx.Guild.GetMemberAsync(AEOTS_SERVER_ID);
                // get reaction
                if (message.Reactions.Any(r => r.Emoji.GetDiscordName() == Program.emojiName))
                {
                    await message.DeleteReactionAsync(message.Reactions.First(r => r.Emoji.GetDiscordName() == Program.emojiName).Emoji, user);
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
        Quote? newestQuote = Program.Database?.Quotes.AsNoTracking()
            .OrderByDescending(q => q.dateTime)
            .FirstOrDefault();

        Logger.Info($"Latest Quote: maxQuoteId = {Program.maxQuoteId}. Actual newest = {(newestQuote?.id.ToString() ?? "null")}");

        DiscordEmbed latestEmbed = await QuoteEmbed(newestQuote?.id ?? Program.maxQuoteId);
        await ctx.Channel.SendMessageAsync(latestEmbed);
        return true;
    }

    private async Task<bool> HandleUsernameOrInvalid(CommandContext ctx, string arg)
    {
        Logger.Info($"quoting by username {arg}");
        var name = arg.ToLowerInvariant();

        ulong? targetUserId = 0;
        try
        {
            targetUserId = TargetUserIdFromName(name);
        }
        catch (InvalidOperationException ioe)
        {
            Logger.Error(ioe.Message);
            Logger.Error(ioe.StackTrace ?? "InvalidOperationException StackTrace Null");
            await ctx.Channel.SendMessageAsync("User not found");
            return false;
        }
        catch (NameScenarioInvalidException nsi) // handle cases where we dont get a single valid user out
        {
            Logger.Error(nsi.Message);
            Logger.Error(nsi.StackTrace ?? "NameScenarioInvalidException StackTrace Null");
            await ctx.Channel.SendMessageAsync("User not found");
            return false;
        }
         
        if (targetUserId is not null)
        {
            long quoteId = await UsernameQuote(targetUserId.Value);
            Logger.Info($"Quote {quoteId} selected");
            DiscordEmbed usernameQuote = await QuoteEmbed(quoteId);
            if (usernameQuote.Title is not null)
            {
                await ctx.Channel.SendMessageAsync(usernameQuote);
            }
            return true;
        } 
        else
        {
            Logger.Error("User Not Found (targetUserId is null)");
            await ctx.Channel.SendMessageAsync("User not found!");
            return false;
        }

    }

    private ulong? TargetUserIdFromName(string name)
    {
        // check nickname, then display name, then username
        // 3 case: nobody with this nickname, one person with this nickname, multiple people with this nickname
        ulong? targetUserId = Program.Members.Count(m => (m.Nickname ?? "").ToLowerInvariant().Equals(name)) switch
            {
                <0 => throw new NameScenarioInvalidException("A negative number of users have this nickname!", NameScenario.NegativeCount, NameType.Nickname),
                1 => Program.Members.Find(m => (m.Nickname ?? "").ToLowerInvariant().Equals(name))?.Id,
                >1 or 0 => Program.Members.Count(m => m.DisplayName.ToLowerInvariant().Equals(name)) switch
                {
                    <0 => throw new NameScenarioInvalidException("A negative number of users have this display name!", NameScenario.NegativeCount, NameType.DisplayName),
                    1 => Program.Members.Find(m => m.DisplayName.ToLowerInvariant().Equals(name))?.Id,
                    >1 or 0 => Program.Members.Count(m => m.Username.ToLowerInvariant().Equals(name)) switch
                    {
                        <0 => throw new NameScenarioInvalidException("A negative number of users have this username!", NameScenario.NegativeCount, NameType.Username),
                        0 => TryGetUserIdFromQuoteList(name), // no users with this username in the server, so loook through quoes list in case they left
                        1 => Program.Members.Find(m => m.Username.ToLowerInvariant().Equals(name))?.Id,
                        >1 => throw new NameScenarioInvalidException("Multiple users have this username!", NameScenario.MultipleUsers, NameType.Username)
                    }
                }
            };
        return targetUserId;
    }

    private ulong? TryGetUserIdFromQuoteList(string username)
    {
        // look through quotes to see if any attached nicknames match our target username
        bool quotesHaveName = Program.GetQuotes().Any(q => q.nick.ToLowerInvariant().Equals(username));
        if (quotesHaveName)
        {
            return Program.GetQuotes().Find(q => q.nick.ToLowerInvariant().Equals(username))?.userId;
        } else throw new NameScenarioInvalidException("No record of this user found in the quote db", NameScenario.ZeroCount, NameType.Nickname);
    }
#endregion

#region Embed/Response Generators
    static async Task<DiscordEmbed> QuoteLeaderboard(int? leaderboardLength)
    {
        var quotes = Program.GetQuotes();
        var topQuotes = quotes.CountBy(q => q.userId).OrderByDescending(kvp => kvp.Value).Take(leaderboardLength ?? 25).ToList();
        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
        StringBuilder listBuilder = new();
        for (int i = 0; i < topQuotes.Count; i++)
        {
            listBuilder.Append($"{i + 1}. <@{topQuotes[i].Key}> ({topQuotes[i].Value} quotes)\n");
        }
        embedBuilder.WithDescription(listBuilder.ToString());
        return embedBuilder.Build();
    }

    static async Task<DiscordEmbed> QuoteEmbed(long id)
    {
        if (id > 0 && id <= Program.maxQuoteId)
        {
            Quote? quote = Program.GetQuotes().Find(q => q.id == id);
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
#endregion
}