
using System.ComponentModel.Composition;
using System.Net;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter3;

[Export(typeof(ICommand))]
sealed partial class Algorithm_3_22Command : CommandAsyncBase
{
    public Algorithm_3_22Command()
        : base("algo-3-22")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var nodeCount = 10;
        var endPoints = PortUtility.GetEndPoints(nodeCount);
        var creationTasks = endPoints.Select(item => Node.CreateAsync(item, cancellationToken)).ToArray();
        await Task.WhenAll(creationTasks);

        await using var nodes = await AsyncDisposableCollection<Node>.CreateAsync(creationTasks);
        await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => AttachNodesAsync(item, endPoints, cancellationToken));
        var runningTasks = nodes.Select(item => item.RunAsync(cancellationToken)).ToArray();
        await Task.WhenAll(runningTasks);

        var tsb = new TerminalStringBuilder();
        for (var i = 0; i < nodes.Count; i++)
        {
            tsb.AppendLine($"{nodes[i]}: {nodes[i].Value}");
            tsb.Append(string.Empty);
        }
        await Out.WriteAsync(tsb.ToString());
    }

    private static async ValueTask AttachNodesAsync(Node node, IEnumerable<EndPoint> endPoints, CancellationToken cancellationToken)
    {
        var others = endPoints.Where(item => item != node.EndPoint);
        await Parallel.ForEachAsync(others, cancellationToken, node.AddNodeAsync);
    }
}
