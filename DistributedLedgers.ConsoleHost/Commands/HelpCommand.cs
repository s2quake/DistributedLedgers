using System.ComponentModel.Composition;
using JSSoft.Library.Commands;

namespace DistributedLedgers.ConsoleHost.Commands;

[Export(typeof(ICommand))]
[Export(typeof(HelpCommand))]
sealed class HelpCommand : HelpCommandBase
{
}
