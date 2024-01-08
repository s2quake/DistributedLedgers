
using System.ComponentModel.Composition;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

[Export(typeof(ICommand))]
sealed partial class Algorithm_4_14Command : CommandAsyncBase
{
    public Algorithm_4_14Command()
        : base("alg-4-14")
    {
    }

    [CommandPropertyRequired(DefaultValue = 4)]
    public int NodeCount { get; set; }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var nodeCount = NodeCount;
        var ports = PortUtility.GetPorts(nodeCount);
        await using var nodes = await Node.CreateManyAsync<Node>(ports, cancellationToken);
        await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => item.RunAsync(cancellationToken));

        var tsb = new TerminalStringBuilder();
        for (var i = 0; i < nodes.Count; i++)
        {
            tsb.AppendLine($"{nodes[i].Index}: {nodes[i].Value}");
            tsb.Append(string.Empty);
        }
        await Out.WriteAsync(tsb.ToString());
        PortUtility.ReleasePorts(ports);
    }
}
