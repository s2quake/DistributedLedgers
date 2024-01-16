
using System.ComponentModel;
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

[Export(typeof(ICommand))]
[Category("Chapter 6")]
sealed partial class PBFT_Command : CommandAsyncBase
{
    public PBFT_Command()
        : base("pbft")
    {
    }

    [CommandProperty("node", 'n', InitValue = 4)]
    public int NodeCount { get; set; }

    [CommandProperty("request", 'r', InitValue = 2)]
    public int RequestCount { get; set; }

    [CommandProperty("client", 'c', InitValue = 1)]
    public int ClientCount { get; set; }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var nc = NodeCount;
        var cc = ClientCount;
        var rc = RequestCount;
        var fc = ByzantineUtility.GetByzantineCount(nc, (n, f) => n == 3 * f + 1);
        var endPoints = PortUtility.GetEndPoints(nc);
        await using var nodes = await PBFT.Node.CreateManyAsync(endPoints, fc, cancellationToken);
        await Out.WriteLineAsync("============ agreement ============");
        Parallel.ForEach(nodes, item => item.Initialize(endPoints, fc, rc * cc));
        var requestTasks = Enumerable.Range(0, cc).Select(c => Task.Run(async () =>
        {
            for (var i = 0; i < rc; i++)
            {
                var r = Random.Shared.Next();
                foreach (var item in nodes)
                {
                    await Task.Delay(Random.Shared.Next(10, 100), cancellationToken);
                    item.Request(r: r, c: c);
                }
            }
        }, cancellationToken)).ToArray();
        await Task.WhenAll(nodes.OrderBy(item => item.GetHashCode()).Select(item => item.RunAsync(cancellationToken)));
        await Task.WhenAll(requestTasks);

        var tsb = new TerminalStringBuilder();
        tsb.AppendLine("============  result  ============");
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var value = node.Value;
            tsb.AppendLine($"{node}");
            for (var j = 0; j < value.Length; j++)
            {
                tsb.AppendLine($"    r: {value[j].r}, c: {value[j].c}");
            }
            tsb.Append(string.Empty);
        }
        tsb.AppendLine("==================================");
        await Out.WriteAsync(tsb.ToString());
    }
}
