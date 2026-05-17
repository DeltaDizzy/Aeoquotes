using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Aeoquotes;

internal class Program
{
    static string token = File.ReadAllLines(@"C:\Users\Emily\Documents\C#\Aeoquotes\token.txt")[0];
    static List<Quote> quotes = [];
    public static long maxQuoteId = 0;

    public static string emojiName = ":thought_balloon:";
    private record struct RawQuote(string id, string nick, string userId, string channel, string channelId, string server, string text, string messageId, long unixTime, DateTime dateTime);

    public static List<DiscordMember> Members {get; private set;} = [];
   
    public static void RemoveQuote(long quoteId)
    {
        quotes.RemoveAll(q => q.id == quoteId);
    }

    public static List<Quote> GetQuotes() => quotes;
    private static async Task Main(string[] args)
    {
        quotes = LoadData();
        using QuotesContext db = new();
        Migrator.OldJsonToEF(@"C:\Users\Emily\Documents\C#\Aeoquotes\quotes.json", db);
        Console.WriteLine("Migration Called");
        
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
                                quotes.Add(new Quote(
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
                                )
                            );
                            await message.CreateReactionAsync(
                                message.Reactions.First(
                                    react => react.Emoji.GetDiscordName().Equals(emojiName)
                                ).Emoji
                            );
                            await channel.SendMessageAsync($"Quote added as #{quotes.Last().id} by {args.User.Username} ({message.JumpLink})");
                            SaveData(quotes);
                            maxQuoteId++;
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
        //await discord.ConnectAsync();
        Console.WriteLine("Connected!");
        var aots = await discord.GetGuildAsync(933937980224196608);
        var members = aots.GetAllMembersAsync();
        await foreach (var user in members)
        {
            Members.Add(user);
        }
        await Task.Delay(-1);
    }

    static void SaveData(List<Quote> quotes)
    {
        string quotesJson = JsonSerializer.Serialize(quotes);
        File.WriteAllTextAsync($"quotes.json", quotesJson);
    }

    static List<Quote> LoadData()
    {
        List<Quote>? realQuotes = [];
        try
        {
            realQuotes = JsonSerializer.Deserialize<List<Quote>>(File.ReadAllText(@"C:\Users\Emily\Documents\C#\Aeoquotes\quotes.json"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            quotes = [];
        }

        if (realQuotes is not null)
        {
            maxQuoteId = realQuotes.Max(q => q.id);
            return realQuotes;
        }
        else return [];
    }
}