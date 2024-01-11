
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
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

    [CommandProperty("repeat", 'r', InitValue = 1)]
    public int RepeatCount { get; set; }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var r = RepeatCount;
        while (r-- > 0 && cancellationToken.IsCancellationRequested != true)
        {
            var n = NodeCount;
            var f = ByzantineUtility.GetByzantineCount(n, (n, f) => f < n / 3.0);
            var ports = PortUtility.GetPorts(n);
            await using var nodes = await Node.CreateManyAsync(ports, f, cancellationToken);
            Console.WriteLine("============ consensus ============");
            await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => item.RunAsync(Random.Shared.Next(), f, cancellationToken));

            var tsb = new TerminalStringBuilder();
            tsb.AppendLine("============  result  ============");
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                tsb.AppendLine($"{node}: {node.Value}");
                tsb.Append(string.Empty);
            }
            tsb.AppendLine("==================================");
            await Out.WriteAsync(tsb.ToString());
            if (nodes.Select(item => item.Value).Distinct().Count() != 1)
                throw new InvalidOperationException("consensus failed.");
        }
    }
}
