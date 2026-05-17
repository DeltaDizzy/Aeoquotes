using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Aeoquotes;

internal class Program
{
    static readonly string token = File.ReadAllLines($"{GetProjectRoot()}/token.txt")[0];
    static List<Quote> quotes = [];
    public static long maxQuoteId = 0;
    public static QuotesContext? Database {get; private set;}
    public static string emojiName = ":thought_balloon:";
    public static List<DiscordMember> Members {get; private set;} = [];
    public static List<Quote> GetQuotes() => quotes;

    private static async Task Main(string[] args)
    {
        using (QuotesContext migratordb = new())
        {
            //Migrator.OldJsonToEF(@$"{GetProjectRoot()}/quotes.json", migratordb);
        }
        
        using QuotesContext db = new();
        Database = db;
        Console.WriteLine($"Database connected: {db.Quotes.Any()}");
        quotes = [.. db.Quotes];
        maxQuoteId = quotes.Max(q => q.id);;
        Console.WriteLine($"Loaded {db.Quotes.Count()} quotes successfully");
        
        DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(token, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMembers);
        builder.ConfigureEventHandlers((handler) =>
        {
            handler.HandleMessageReactionAdded(async (client, args) =>
            {
                Console.WriteLine($"Reaction Added: {args.Emoji.GetDiscordName()}");
                if (args.Message.Reactions.Any(react => react.Emoji.GetDiscordName().Equals(emojiName)))
                {
                    if (!quotes.Any(q => q.messageId == args.Message.Id)) // if the subset of the quotes list where the message id matches this message is empty, we havent quoted it yet
                    {
                        // get the message ourselves because a lot of the fields are null
                        DiscordChannel channel = await client.GetChannelAsync(args.Channel.Id);
                        DiscordMessage message = await channel.GetMessageAsync(args.Message.Id);
                        if (message.Author is not null)
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
                            Console.WriteLine(newQuotes);
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
                            Console.WriteLine("Author is null!");
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
        Console.WriteLine("Connected!");
        var aots = await discord.GetGuildAsync(933937980224196608);
        //SaveData(await FixupData(quotes, aots), "uotes.json");
        var members = aots.GetAllMembersAsync();
        await foreach (var user in members)
        {
            Members.Add(user);
        }
        await Task.Delay(-1);
    }

    public static string GetProjectRoot([CallerFilePath] string callerFilePath = "")
    {
        // callerFilePath contains the absolute path to THIS source file on compile time.
        
        var directory = Path.GetDirectoryName(callerFilePath);
        
        // Traverse up until we find the folder containing the .csproj file
        while (directory != null)
        {
            if (Directory.GetFiles(directory, "*.csproj").Length > 0)
            {
                return directory;
            }
            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find the project root directory containing a .csproj file.");
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