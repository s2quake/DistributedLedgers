
using System.ComponentModel.Composition;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

[Export(typeof(ICommand))]
sealed partial class Algorithm_4_9Command : CommandAsyncBase
{
    public Algorithm_4_9Command()
        : base("alg-4-9")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var nodeCount = 10;
        var ports = PortUtility.GetPorts(nodeCount);
        await using var nodes = await Node.CreateManyAsync(ports, cancellationToken);
        await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => item.RunAsync(cancellationToken));

        var tsb = new TerminalStringBuilder();
        for (var i = 0; i < nodes.Count; i++)
        {
            tsb.AppendLine($"{nodes[i].Port}: {nodes[i].Value}");
            tsb.Append(string.Empty);
        }
        await Out.WriteAsync(tsb.ToString());
    }
}
