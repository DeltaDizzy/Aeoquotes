using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using log4net;
using Microsoft.EntityFrameworkCore;

namespace Aeoquotes.Commands;

[Command("quote")]
[TextAlias("q")]
public partial class QuoteCommands
{
    private static readonly ulong AEOTS_SERVER_ID = 1503994723118088292;
    private static readonly ILog Logger = LogManager.GetLogger(typeof(QuoteCommands));

    [Command("random")]
    private async Task HandleRandom(CommandContext ctx)
    {
        DiscordEmbed embed = await RandomQuote();
        if (embed.Title is not null)
        {
            await ctx.Channel.SendMessageAsync(embed);
        }
    }

    [Command("stats")]
    private async Task HandleStats(CommandContext ctx, [RemainingText] string? data = null)
    {
        DiscordEmbed stats;
        int leaderboardLength = 25;
        LeaderboardType leaderboardType = LeaderboardType.Quotee;
        if (data is not null)
        {
            var args = data.Split(" ");
            leaderboardLength = args.Select(s => int.TryParse(s, out int n) ? n : 25).FirstOrDefault(25);
            leaderboardType = GetLeaderboardType(args);
        }
        Logger.Info($"Generating leaderboard, length={leaderboardLength}, type = {leaderboardType}");
        stats = await QuoteLeaderboard(leaderboardLength, leaderboardType);
        await ctx.Channel.SendMessageAsync(stats);
    }

    [Command("delete")]
    [TextAlias("remove")]
    private async Task HandleDelete(CommandContext ctx, string target)
    {
        if (long.TryParse(target, out long quoteToRemove) && ctx.Guild is not null)
        {
            if (Program.GetQuotes().Count <= quoteToRemove && quoteToRemove > 0)
            {
                Logger.Info($"deleting quote {quoteToRemove}");
                // need to remove our reaction
                Quote? quote = Program.GetQuotes().Find(q => q.id == quoteToRemove);
                if (quote is null)
                {
                    await ctx.Channel.SendMessageAsync("Quote not found");
                    return;
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
    }

    [Command("latest")]
    private async Task HandleLatest(CommandContext ctx)
    {
        Quote? newestQuote = Program.Database?.Quotes.AsNoTracking()
            .OrderByDescending(q => q.id)
            .FirstOrDefault();

        Logger.Info($"Latest Quote: maxQuoteId = {Program.maxQuoteId}. Actual newest = {newestQuote?.id.ToString() ?? "null"}");

        DiscordEmbed latestEmbed = await QuoteEmbed(newestQuote?.id ?? Program.maxQuoteId);
        await ctx.Channel.SendMessageAsync(latestEmbed);
    }

    [Command("default")]
    [DefaultGroupCommand]
    private async Task HandleDirectArgs(CommandContext context, [RemainingText] string? arg = null)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            await HandleRandom(context);
            return;
        }
        var cmdargs = arg.Split(" ");
        // if its a number we want to fetch that quote
        if (int.TryParse(cmdargs[0], out int id))
        {
            Logger.Info($"quoting by number: {id}");
            DiscordEmbed quote = await QuoteEmbed(id);
            await context.Channel.SendMessageAsync(quote);
        }
        else
        {
            // username or invalid arg
            await HandleUsernameOrInvalid(context, arg);
        }
    }

    //[Command("by")]
    [Description("Returns a quote that was quoted by the given user")]
    private async Task QuotedBy(CommandContext context, string target)
    {
        DiscordEmbed embed = await QuoteByEmbed(target);
        await context.Channel.SendMessageAsync(embed);
    }


    private async Task<bool> HandleUsernameOrInvalid(CommandContext ctx, string arg)
    {
        Logger.Info($"quoting by username {arg}");
        var name = arg.ToLowerInvariant();
        ulong? targetUserId = 0;
        try
        {
            if (name is "me")
            {
                name = ctx.Member?.Username;
            }
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
        catch (NullReferenceException nre)
        {
            Logger.Error(nre.Message);
            Logger.Error(nre.StackTrace ?? "NullReferenceException StackTrace Null");
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
}