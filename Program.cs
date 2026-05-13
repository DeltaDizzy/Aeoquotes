using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Aeoquotes;
internal class Program
{
    static string token = File.ReadAllLines("token.txt")[0];
    static List<Quote> quotes = [];
    public static long maxQuoteId = 0;
    public static Settings settings {get; private set; } = new Settings(":thought_balloon:", "&");
    public record struct Quote(long id, string nick, ulong userId, string channel, ulong channelId, ulong server, string text, ulong messageId, long unixTime, DateTime dateTime);
    public record struct Settings(string reactName = ":thought_balloon:", string prefix = "&");
    private record struct RawQuote(string id, string nick, string userId, string channel, string channelId, string server, string text, string messageId, long unixTime, DateTime dateTime);


    public static void UpdateSettings(Settings newSettings) => UpdateSettings(newSettings.reactName, newSettings.prefix);

    public static void UpdateSettings(string reactName, string prefix)
    {
        settings = new Settings(reactName, prefix);
        SaveData(quotes, settings);
    }

    public static void RemoveQuote(long quoteId)
    {

        quotes.RemoveAll(q => q.id == quoteId);
    }

    public static List<Quote> GetQuotes() => quotes;
    private static async Task Main(string[] args)
    {
        (quotes, settings) = LoadData();
        DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(token, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents);
        builder.ConfigureEventHandlers((handler) =>
        {
            handler.HandleMessageReactionAdded(async (client, args) =>
            {
                Console.WriteLine($"Reaction Added: {args.Emoji.GetDiscordName()}");
                if (args.Message.Reactions.Any(react => react.Emoji.GetDiscordName().Equals(settings.reactName)))
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
                                    author.Id,
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
                                    react => react.Emoji.GetDiscordName().Equals(settings.reactName)
                                ).Emoji
                            );
                            await channel.SendMessageAsync($"Quote added as #{quotes.Last().id}");
                            SaveData(quotes, settings);
                            maxQuoteId++;
                        }
                        else
                        {
                            Console.WriteLine("Author is null!");
                        }

                    }
                }
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
        await Task.Delay(-1);
    }

    static void SaveData(List<Quote> quotes, Settings settings)
    {
        string quotesJson = JsonSerializer.Serialize(quotes);
        string settingsJson = JsonSerializer.Serialize(settings);
        File.WriteAllTextAsync($"quotes.json", quotesJson);
        File.WriteAllTextAsync($"settings.json", settingsJson);
    }

    static (List<Quote>, Settings) LoadData()
    {
        
        List<RawQuote>? rawQuotes = null;
        List<Quote>? realQuotes = [];
        Settings newSettings;
        try
        {
            realQuotes = JsonSerializer.Deserialize<List<Quote>>(File.ReadAllText("quotes.json"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            quotes = [];
        }

        try
        {
            newSettings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"));
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.Message);
            newSettings = new Settings(":thought_balloon:", "&");
        }

        if (rawQuotes is not null && realQuotes is not null)
        {
            foreach (RawQuote q in rawQuotes)
            {
                if(
                    long.TryParse(q.id, out long quoteId) &&
                    ulong.TryParse(q.userId, out ulong quoteUserId) &&
                    ulong.TryParse(q.channelId, out ulong quoteChannelId) &&
                    ulong.TryParse(q.server, out ulong quoteServerId) &&
                    ulong.TryParse(q.messageId, out ulong quoteMessageId)
                )
                {
                    realQuotes.Add(
                        new Quote(
                            quoteId,
                            q.nick,
                            quoteUserId,
                            q.channel,
                            quoteChannelId,
                            quoteServerId,
                            q.text,
                            quoteMessageId,
                            q.unixTime,
                            q.dateTime
                        )
                    );
                }
                else
                {
                    Console.WriteLine($"Failed to load quote #{q.id}");
                }
                
            }
        }

        if (realQuotes is not null)
        {
            maxQuoteId = realQuotes.Max(q => q.id);
            return (realQuotes, newSettings);
        }
        else return ([], newSettings);
    }
}