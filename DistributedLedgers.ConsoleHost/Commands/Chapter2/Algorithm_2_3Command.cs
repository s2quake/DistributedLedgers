
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

[Export(typeof(ICommand))]
sealed class Algorithm_2_3Command : CommandAsyncBase
{
    public Algorithm_2_3Command()
        : base("alg-2-3")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serverService = new ServerDataService();
        var clientService = new ClientDataService();
        await using var server = await Server.CreateAsync(serverService, cancellationToken);
        await using var client = await Client.CreateAsync(clientService, cancellationToken);
        Parallel.ForEach(Enumerable.Range(0, 100), i =>
        {
            clientService.SendMessage($"message {i}");
        });
    }

    public interface IDataService
    {
        [ServerMethod(IsOneWay = true)]
        void SendMessage(string message);
    }

    sealed class ServerDataService : ServerService<IDataService>, IDataService
    {
        public void SendMessage(string message)
        {
            Console.WriteLine($"server receive: {message}");
        }
    }

    sealed class ClientDataService : ClientService<IDataService>, IDataService
    {
        public void SendMessage(string message)
        {
            Server.SendMessage(message);
            Console.WriteLine($"client send: {message}");
        }
    }
}
