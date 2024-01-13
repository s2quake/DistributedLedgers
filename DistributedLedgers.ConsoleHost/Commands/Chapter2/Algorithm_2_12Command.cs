using System.ComponentModel;
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

[Export(typeof(ICommand))]
[Category("Chapter 2")]
sealed partial class Algorithm_2_12Command : CommandAsyncBase
{
    public Algorithm_2_12Command()
        : base("algo-2-12")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serverCount = 4;
        var serverEndPoints = PortUtility.GetEndPoints(serverCount);
        var serverServices = new ServerMessageService[serverCount];
        await using var servers = new AsyncDisposableCollection<Server>(capacity: serverCount);
        Out.WriteLine("Nodes initializing.");
        for (var i = 0; i < serverCount; i++)
        {
            serverServices[i] = new ServerMessageService($"server{i}");
            servers.Add(await Server.CreateAsync(serverEndPoints[i], serverServices[i], cancellationToken));
        }
        var clientCount = 3;
        await using var clients = new AsyncDisposableCollection<Client>(capacity: clientCount);
        for (var i = 0; i < clientCount; i++)
        {
            clients.Add(await Client.CreateAsync($"client {i}", serverEndPoints, cancellationToken));
        }
        Out.WriteLine("Nodes initialized.");

        await Parallel.ForEachAsync(clients, async (item, cancellationToken) =>
        {
            await item.RunAsync($"command of client {item.Name}", cancellationToken);
        });
    }
}
