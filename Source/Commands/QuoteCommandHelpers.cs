namespace Aeoquotes.Commands;

public partial class QuoteCommands
{

    protected enum LeaderboardType
    {
        Quotee,
        Quoter,
        Weekly_Quoted
    }

    private LeaderboardType GetLeaderboardType(string[] args)
    {
        foreach (var item in args)
        {
            Console.WriteLine(item);
        }
        // args[0] can be an int (if quoted leaderboard) or a leaderboard type
        return args[0] switch
        {
            "quoted" => LeaderboardType.Quotee,
            "quoter" or "quoters" => LeaderboardType.Quoter,
            "weekly" => LeaderboardType.Weekly_Quoted,
            _  => LeaderboardType.Quotee, // if nothing matches assume it was a number/ignore it
        };
    }

    private static ulong? TargetUserIdFromName(string name)
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

    private static ulong? TryGetUserIdFromQuoteList(string username)
    {
        // look through quotes to see if any attached nicknames match our target username
        bool quotesHaveName = Program.GetQuotes().Any(q => q.nick.ToLowerInvariant().Equals(username));
        if (quotesHaveName)
        {
            return Program.GetQuotes().Find(q => q.nick.ToLowerInvariant().Equals(username))?.userId;
        } else throw new NameScenarioInvalidException("No record of this user found in the quote db", NameScenario.ZeroCount, NameType.Nickname);
    }
}