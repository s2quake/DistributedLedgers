
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

[Export(typeof(ICommand))]
sealed partial class Algorithm_6_2Command : CommandAsyncBase
{
    public Algorithm_6_2Command()
        : base("algo-6-2")
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
            var f = ByzantineUtility.GetByzantineCount(n, (n, f) => f < n / 2.0);
            var ports = PortUtility.GetPorts(n);
            var p = Random.Shared.Next(n);
            await using var nodes = await Node.CreateManyAsync(ports, f, cancellationToken);
            await Out.WriteLineAsync("============ agreement ============");
            await Task.WhenAll(nodes.OrderBy(item => item.GetHashCode()).Select(item => item.RunAsync(p, f, cancellationToken)));

            var tsb = new TerminalStringBuilder();
            tsb.AppendLine("============  result  ============");
            for (var i = 0; i < nodes.Count; i++)
            {
                var primary = p == i ? "p" : " ";
                var node = nodes[i];
                tsb.AppendLine($"{primary} {node}: {node.Value}");
                tsb.Append(string.Empty);
            }
            tsb.AppendLine("==================================");
            await Out.WriteAsync(tsb.ToString());
        }
    }
}
