
using System.ComponentModel;
using System.ComponentModel.Composition;
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

    [CommandPropertyRequired(DefaultValue = 4)]
    public int NodeCount { get; set; }

    [CommandProperty("repeat", 'r', InitValue = 1)]
    public int RepeatCount { get; set; }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        // var r = RepeatCount;
        // while (r-- > 0 && cancellationToken.IsCancellationRequested != true)
        // {
        var n = NodeCount;
        // var f = ByzantineUtility.GetByzantineCount(n, (n, f) => n == 3 * f + 1);
        var f = 0;
        var endPoints = PortUtility.GetEndPoints(n);
        await using var nodes = await PBFT.Node.CreateManyAsync(endPoints, f, cancellationToken);
        await Out.WriteLineAsync("============ agreement ============");
        Parallel.ForEach(nodes, item => item.Initialize(endPoints, f));
        Parallel.ForEach(Enumerable.Range(0, 10), c =>
        {
            var r = Random.Shared.Next();
            foreach (var item in nodes)
            {
                item.Request(r: r, c: c);
            }
        });
        await Task.WhenAll(nodes.OrderBy(item => item.GetHashCode()).Select(item => item.RunAsync(cancellationToken)));

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
        // }
    }
}
