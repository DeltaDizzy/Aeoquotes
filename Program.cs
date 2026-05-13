using System.Text.Json;
using DSharpPlus;

namespace Aeoquotes;
internal class Program
{
    static string token = File.ReadAllLines("token.txt")[0];
    static string quoteReactName = "";
    static string cmdPrefix = "";
    static List<Quote> quotes = [];
    static Settings settings;
    public record struct Quote(long id, string nick, ulong userId, string channel, ulong channelId, ulong server, string text, ulong messageId, long unixTime, DateTime dateTime);
    public record struct Settings(string reactName, string prefix);

    public static void UpdateSettings(Settings newSettings) => UpdateSettings(newSettings.reactName, newSettings.prefix);

    public static void UpdateSettings(string reactName, string prefix)
    {
        quoteReactName = reactName;
        cmdPrefix = prefix;
        SaveData(quotes, new Settings(reactName, prefix));
    }

    public static void RemoveQuote(long quoteId)
    {
        quotes.RemoveAll(q => q.id == quoteId);
    }

    public static List<Quote> GetQuotes() => quotes;
    private static async Task Main(string[] args)
    {
        (quotes, settings) = LoadData();
        DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(token, DiscordIntents.AllUnprivileged);
        builder.ConfigureEventHandlers((handler) =>
        {
            handler.HandleMessageReactionAdded(async (client, args) =>
            {
                if (args.Message.Reactions.Any(react => react.Emoji.Name.Equals(quoteReactName)))
                {
                    if (!quotes.Any(q => q.messageId == args.Message.Id)) // if the subset of the quotes list where the message id matches this message is empty, we havent quoted it yet
                    {
                        quotes.Add(new Quote(
                            quotes.Count + 1,
                            args.Message.Author.Username,
                            args.Message.Author.Id,
                            args.Channel.Name,
                            args.Channel.Id,
                            args.Guild.Id,
                            args.Message.Content,
                            args.Message.Id,
                            args.Message.Timestamp.ToUnixTimeSeconds(),
                            new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(args.Message.Timestamp.ToUnixTimeSeconds())
                            )
                        );
                    }
                }
            });
        });
        DiscordClient discord = builder.Build();

        await discord.ConnectAsync();
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
        List<Quote>? quotes = JsonSerializer.Deserialize<List<Quote>>(File.ReadAllText("quotes.json"));
        Settings settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"));
        if (quotes is not null)
        {
            return (quotes, settings);
        }
        else return ([], settings);
    }
}