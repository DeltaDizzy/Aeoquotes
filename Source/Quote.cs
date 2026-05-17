namespace Aeoquotes;

public record Quote(long id, string nick, ulong userId, string channel, ulong channelId, ulong server, string text, ulong messageId, long unixTime, DateTime dateTime);
