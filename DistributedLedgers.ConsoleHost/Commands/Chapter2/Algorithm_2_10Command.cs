using System.ComponentModel;
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

[Export(typeof(ICommand))]
[Category("Chapter 2")]
sealed partial class Algorithm_2_10Command : CommandAsyncBase
{
    public Algorithm_2_10Command()
        : base("algo-2-10")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serverEndPoints = PortUtility.GetEndPoints(2);
        var serverService1 = new ServerMessageService("server1");
        var serverService2 = new ServerMessageService("server2");
        Out.WriteLine("Nodes initializing.");
        await using var server1 = await Server.CreateAsync(serverEndPoints[0], serverService1, cancellationToken);
        await using var server2 = await Server.CreateAsync(serverEndPoints[1], serverService2, cancellationToken);
        await using var client1 = await Client.CreateAsync(serverEndPoints, cancellationToken);
        await using var client2 = await Client.CreateAsync(serverEndPoints, cancellationToken);
        await using var client3 = await Client.CreateAsync(serverEndPoints, cancellationToken);
        Out.WriteLine("Nodes initialized.");

        await Task.WhenAll(
        [
            client1.RunAsync("Hello World 1", cancellationToken),
            client2.RunAsync("Hello World 2", cancellationToken),
            client3.RunAsync("Hello World 3", cancellationToken),
        ]);
    }
}
