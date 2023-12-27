using System.ComponentModel.Composition;
using JSSoft.Library.Commands;

namespace DistributedLedgers.ConsoleHost.Commands;

[Export(typeof(ICommand))]
[Export(typeof(VersionCommand))]
sealed class VersionCommand : VersionCommandBase
{
}
