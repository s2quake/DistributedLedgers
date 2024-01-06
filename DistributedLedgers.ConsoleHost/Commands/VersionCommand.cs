using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands;

[Export(typeof(ICommand))]
[Export(typeof(VersionCommand))]
sealed class VersionCommand : VersionCommandBase
{
}
