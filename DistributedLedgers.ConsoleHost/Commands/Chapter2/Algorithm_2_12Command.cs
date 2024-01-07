using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

[Export(typeof(ICommand))]
sealed partial class Algorithm_2_12Command : CommandAsyncBase
{
    public Algorithm_2_12Command()
        : base("alg-2-12")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serverCount = 4;
        var serverPorts = PortUtility.GetPorts(serverCount);
        var serverServices = new ServerMessageService[serverCount];
        await using var servers = new AsyncDisposableCollection<SimpleServer>(capacity: serverCount);
        Out.WriteLine("Nodes initializing.");
        for (var i = 0; i < serverCount; i++)
        {
            serverServices[i] = new ServerMessageService($"server{i}");
            servers.Add(await SimpleServer.CreateAsync(serverPorts[i], serverServices[i], cancellationToken));
        }
        var clientCount = 3;
        await using var clients = new AsyncDisposableCollection<Client>(capacity: clientCount);
        for (var i = 0; i < clientCount; i++)
        {
            clients.Add(await Client.CreateAsync($"client {i}", serverPorts, cancellationToken));
        }
        Out.WriteLine("Nodes initialized.");

        await Parallel.ForEachAsync(clients, async (item, cancellationToken) =>
        {
            await item.RunAsync($"command of client {item.Name}", cancellationToken);
        });
    }
}
