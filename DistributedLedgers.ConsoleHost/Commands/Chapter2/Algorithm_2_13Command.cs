using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;
using JSSoft.Terminals;
using JSSoft.Communication.Threading;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

[Export(typeof(ICommand))]
sealed partial class Algorithm_2_13Command : CommandAsyncBase
{
    public Algorithm_2_13Command()
        : base("alg-2-13")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        while (true)
        {
            var serverCount = 1;
            var clientCount = 100;
            var serverPorts = PortUtility.GetPorts(serverCount);
            var serverServices = Enumerable.Range(0, serverCount).Select(item => new ServerMessageService($"server {item}")).ToArray();
            Out.WriteLine("Nodes initializing.");
            await using var servers = await SimpleServer.CreateManyAsync(serverPorts, serverServices, cancellationToken);
            await using var clients = await Client.CreateManyAsync(clientCount, serverPorts, cancellationToken);
            Out.WriteLine("Nodes initialized.");

            await Parallel.ForEachAsync(clients, async (item, cancellationToken) =>
            {
                await item.RunAsync($"command of client {item.Name}", cancellationToken);
            });

            await Out.WriteLineAsync();
            var tsb = new TerminalStringBuilder();
            for (var i = 0; i < serverServices.Length; i++)
            {
                var executedCommands = serverServices[i].ExecutedCommands;
                tsb.IsBold = true;
                tsb.AppendLine(serverServices[i].Name);
                tsb.IsBold = false;
                for (var j = 0; j < executedCommands.Length; j++)
                {
                    tsb.AppendLine($"    {executedCommands[j]}");
                }
            }
            await Out.WriteAsync(tsb.ToString());
            GC.Collect();
        }
    }
}
