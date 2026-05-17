using System;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace Aeoquotes;

public class HelpFormatter : BaseHelpFormatter
{
    protected DiscordEmbedBuilder embed;
    protected StringBuilder strBuilder;
    public HelpFormatter(CommandContext ctx) : base(ctx)
    {
        embed = new();
        strBuilder = new();

        // dependency injection here
        // other init here
    }

    public override CommandHelpMessage Build()
    {
        return new CommandHelpMessage(embed: embed);
    }

    public override BaseHelpFormatter WithCommand(Command command)
    {
        embed.AddField(command.Name, command.Description ?? "");
        strBuilder.AppendLine($"{command.Name} - {command.Description}");
        return this;
    }

    public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
    {
        foreach (var cmd in subcommands)
        {
            embed.AddField(cmd.Name, cmd.Description ?? "");
            strBuilder.AppendLine($"{cmd.Name} - {cmd.Description}");
        }
        return this;
    }
}
