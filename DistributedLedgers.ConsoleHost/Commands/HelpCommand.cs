using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands;

[Export(typeof(ICommand))]
[Export(typeof(HelpCommand))]
sealed class HelpCommand : HelpCommandBase
{
}
