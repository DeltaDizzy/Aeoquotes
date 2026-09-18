using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
namespace Aeoquotes;

internal class Program
{
    static List<Quote> quotes = [];
    public static long maxQuoteId = 0;
    public static QuotesContext? Database {get; private set;}
    public static string emojiName = ":thought_balloon:";
    public static List<DiscordMember> Members {get; private set;} = [];
    public static List<Quote> GetQuotes() => quotes;

    public static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

    private static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        XmlConfigurator.Configure(new FileInfo($"{GetProjectRoot()}/log4netconfig.xml"));        

        using (QuotesContext migratordb = new())
        {
            //Migrator.OldJsonToEF(@$"{GetProjectRoot()}/quotes.json", migratordb);
        }
        
        using QuotesContext db = new();
        Database = db;
        Logger.Info($"Database connected: {db.Quotes.Any()}");
        quotes = [.. db.Quotes];
        maxQuoteId = quotes.Max(q => q.id);;
        Logger.Info($"Loaded {db.Quotes.Count()} quotes successfully");
        
        DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(File.ReadAllText($"{GetProjectRoot()}/token.txt"), DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMembers);
        builder.ConfigureEventHandlers((handler) =>
        {
            handler.HandleMessageReactionAdded(async (client, args) =>
            {
                if (args.Message.Reactions.Any(react => react.Emoji.GetDiscordName().Equals(emojiName)))
                {
                    if (!quotes.Any(q => q.messageId == args.Message.Id)) // if the subset of the quotes list where the message id matches this message is empty, we havent quoted it yet
                    {
                        // get the message ourselves because a lot of the fields are null
                        DiscordChannel channel = await client.GetChannelAsync(args.Channel.Id);
                        DiscordMessage message = await channel.GetMessageAsync(args.Message.Id);
                        if (message.Author is not null && !message.Author.IsBot)
                        {
                            DiscordMember author = await channel.Guild.GetMemberAsync(message.Author.Id);
                            Quote newQuote = new Quote(
                                quotes.Last().id + 1,
                                author.DisplayName,
                                userId: author.Id,
                                channel.Name,
                                channel.Id,
                                channel.Guild.Id,
                                message.Content,
                                message.Id,
                                message.CreationTimestamp.ToUnixTimeSeconds(),
                                new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(message.CreationTimestamp.ToUnixTimeSeconds()).ToUniversalTime()
                            );

                            quotes.Add(newQuote);
                            db.Add(newQuote);
                            int newQuotes = db.SaveChanges();
                            Logger.Info($"{newQuotes} quotes saved");
                            if (newQuotes is 1)
                            {
                                maxQuoteId++;
                                await message.CreateReactionAsync(
                                    message.Reactions.First(
                                        react => react.Emoji.GetDiscordName().Equals(emojiName)
                                    ).Emoji
                                );
                                await channel.SendMessageAsync($"Quote added as #{quotes.Last().id} by {args.User.Username} ({message.JumpLink})");
                            }
                            else
                            {
                                await channel.SendMessageAsync("Quote failed to add");
                            }
                        }
                        else
                        {
                            Logger.Warn("Author is null!");
                        }

                    }
                }
            });

            handler.HandleGuildMemberAdded(async (client, args) =>
            {
                Members.Add(args.Member);
            });
        });
        builder = builder.UseCommandsNext((cnb) =>
            {
                cnb.RegisterCommands<QuoteCommands>();
            }, 
            new CommandsNextConfiguration()
            {
                StringPrefixes = ["!"]
            }
        );

        DiscordClient discord = builder.Build();
        
        await discord.ConnectAsync();
        Logger.Info("Connected!");
        var aots = await discord.GetGuildAsync(933937980224196608);
        //SaveData(await FixupData(quotes, aots), "uotes.json");
        var members = aots.GetAllMembersAsync();
        await foreach (var user in members)
        {
            Members.Add(user);
        }
        await Task.Delay(-1);
    }

    public static string GetProjectRoot()
    {
        string projectRoot = System.IO.Directory.GetCurrentDirectory();

        Logger.Info($"Assuming root folder is {projectRoot}");

        return projectRoot;
    }

    public static void RemoveQuote(long quoteId)
    {
        Quote? toRemove = quotes.Find(q => q.id == quoteId);
        if (toRemove is not null)
        {
            quotes.Remove(toRemove);
            var removed = Database?.Remove(toRemove);
            Database?.SaveChanges();
        }
    }
}