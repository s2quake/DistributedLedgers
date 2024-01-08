
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
        await using var server = await SimpleServer.CreateAsync(serverService, cancellationToken);
        await using var client = await SimpleClient.CreateAsync(clientService, cancellationToken);
        for (var i = 0; i < 10; i++)
        {
            clientService.SendMessage($"message {i}");
        }
    }

    public interface IDataService
    {
        [OperationContract]
        void SendMessage(string message);
    }

    sealed class ServerDataService : ServerServiceHost<IDataService>, IDataService
    {
        public void SendMessage(string message)
        {
            Console.Out.WriteLine($"server: {message}");
        }
    }

    sealed class ClientDataService : ClientServiceHost<IDataService>, IDataService
    {
        public void SendMessage(string message)
        {
            Console.Out.WriteLine($"client: {message}");
            Service.SendMessage(message);
        }
    }
}