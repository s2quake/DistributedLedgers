
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;
using JSSoft.Library.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

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
        await using var server = await SimpleServer.CreateAsync(serverService);
        await using var client = await SimpleClient.CreateAsync(clientService);
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

    sealed class ServerDataService : ServerServiceHostBase<IDataService>, IDataService
    {
        public void SendMessage(string message)
        {
            Console.Out.WriteLine($"server: {message}");
        }
    }

    sealed class ClientDataService : ClientServiceHostBase<IDataService>, IDataService
    {
        public void SendMessage(string message)
        {
            Console.Out.WriteLine($"client: {message}");
            Service.SendMessage(message);
        }
    }
}
