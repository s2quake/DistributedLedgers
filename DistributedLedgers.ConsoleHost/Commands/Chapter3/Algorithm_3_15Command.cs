
using System.ComponentModel.Composition;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter3;

[Export(typeof(ICommand))]
sealed partial class Algorithm_3_15Command : CommandAsyncBase
{
    public Algorithm_3_15Command()
        : base("alg-3-15")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var nodeCount = 10;
        var ports = PortUtility.GetPorts(nodeCount);
        var creationTasks = ports.Select(item => Node.CreateAsync(item, cancellationToken)).ToArray();
        await Task.WhenAll(creationTasks);

        await using var nodes = await AsyncDisposableCollection<Node>.CreateAsync(creationTasks);
        await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => AttachNodesAsync(item, nodes, cancellationToken));
        var runningTasks = nodes.Select(item => item.RunAsync(cancellationToken)).ToArray();
        await Task.WhenAll(runningTasks);

        var tsb = new TerminalStringBuilder();
        for (var i = 0; i < nodes.Count; i++)
        {
            tsb.AppendLine($"{nodes[i].Port}: {nodes[i].Value}");
            tsb.Append(string.Empty);
        }
        await Out.WriteAsync(tsb.ToString());
        PortUtility.ReleasePorts(ports);
    }

    private static async ValueTask AttachNodesAsync(Node node, IEnumerable<Node> nodes, CancellationToken cancellationToken)
    {
        var others = nodes.Where(item => item != node);
        await Parallel.ForEachAsync(others, cancellationToken, node.AddNodeAsync);
    }
}
