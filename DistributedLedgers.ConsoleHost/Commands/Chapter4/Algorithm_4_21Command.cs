
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

[Export(typeof(ICommand))]
sealed partial class Algorithm_4_21Command : CommandAsyncBase
{
    public Algorithm_4_21Command()
        : base("algo-4-21")
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
            var f = ByzantineUtility.GetByzantineCount(n, (n, f) => f < n / 10.0);
            var endPoints = PortUtility.GetEndPoints(n);
            await using var nodes = await Node.CreateManyAsync(endPoints, f, cancellationToken);
            await Out.WriteLineAsync("============ consensus ============");
            await Task.WhenAll(nodes.OrderBy(item => item.GetHashCode()).Select(item => item.RunAsync(Random.Shared.Next() % 2 == 0, f, cancellationToken)));

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
