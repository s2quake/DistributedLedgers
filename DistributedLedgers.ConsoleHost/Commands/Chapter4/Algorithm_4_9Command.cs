
using System.ComponentModel;
using System.ComponentModel.Composition;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

[Export(typeof(ICommand))]
[Category("Chapter 4")]
sealed partial class Algorithm_4_9Command : CommandAsyncBase
{
    public Algorithm_4_9Command()
        : base("algo-4-9")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var nodeCount = 10;
        var endPoints = PortUtility.GetEndPoints(nodeCount);
        await using var nodes = await Node.CreateManyAsync(endPoints, cancellationToken);
        await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => item.RunAsync(cancellationToken));

        var tsb = new TerminalStringBuilder();
        for (var i = 0; i < nodes.Count; i++)
        {
            tsb.AppendLine($"{nodes[i]}: {nodes[i].Value}");
            tsb.Append(string.Empty);
        }
        await Out.WriteAsync(tsb.ToString());
    }
}
