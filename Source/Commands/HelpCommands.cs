using System.ComponentModel;
using System.Reflection;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;


namespace Aeoquotes.Commands;

public class HelpCommands
{

    [Command("help")]
    [Description("Displays all available commands")]
    public async Task Help(CommandContext context, [Description("Specific command to get help for")] string? commandName = null)
    {
        CommandsExtension commandsExtension = context.Extension;

        if (string.IsNullOrWhiteSpace(commandName)) // general help embed
        {
            StringBuilder quoteHelpBuilder = new();
            DiscordEmbedBuilder embed = new()
            {
                Title = "Aeoquotes Help",
                Description = "Syntax of all quote commands",
                Color = DiscordColor.White
            };

            foreach (var command in commandsExtension.Commands.Values)
            {
                if (command.Subcommands.Count is 0)
                {
                    continue;
                }
                var subcommands = command.Subcommands;
                foreach (var subcommand in subcommands)
                {
                    //quoteHelpBuilder.Append($"`!quote {subcommand.Name}` - {string.Join(" ", subcommand.Parameters[0].)}");
                }
            }
            
            // Name: quote, Parent: , Id: 01M4SK3YKKG9SXPJC28GPHQM3H
            // Name: random, Parent: quote, Id: 01M4SK3YKJHFERFKTA97PCYJZ4
            // Name: stats, Parent: quote, Id: 01M4SK3YKJG4D2SCE4SN9DHA17
            // Name: delete, Parent: quote, Id: 01M4SK3YKK7ZMG8JA7ZD62Y8HD
            // Name: latest, Parent: quote, Id: 01M4SK3YKKW6X080V1DMGVHSM9
            // Name: quote, Parent: quote, Id: 01M4SK3YKK8HRJTJ3YJP1AMR9S
            // Name: help, Parent: , Id: 01M4SK3YKK15VQ1Z3Q0K5NE22H

            

            
            
        }   
    }

    private string CommandString(Command command)
    {
        return $"`{command.Name}` - ";
    }
}

/*
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
*/
