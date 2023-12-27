using System.ComponentModel.Composition;
using JSSoft.Library.Commands;
using JSSoft.Library.Terminals;
using DistributedLedgers.ConsoleHost.Commands;

namespace DistributedLedgers.ConsoleHost;

[Export(typeof(CommandContext))]
[CommandSummary("Provides a prompt for input and execution of commands.")]
[CommandDescription("REPL for DistributedLedgers")]
sealed class CommandContext : CommandContextBase
{
    protected override ICommand HelpCommand { get; }

    protected override ICommand VersionCommand { get; }

    [ImportingConstructor]
    public CommandContext([ImportMany] IEnumerable<ICommand> commands, HelpCommand helpCommand, VersionCommand versionCommand)
        : base(commands)
    {
        HelpCommand = helpCommand;
        VersionCommand = versionCommand;
    }

    protected override void OnEmptyExecute()
    {
        var tsb = new TerminalStringBuilder
        {
            Foreground = TerminalColorType.BrightGreen,
        };
        tsb.AppendLine("Type '--help | -h' for usage.");
        tsb.AppendLine("Type 'exit' to exit application.");
        Out.Write(tsb.ToString());
    }
}
