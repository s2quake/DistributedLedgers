
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

[Export(typeof(ICommand))]
sealed class Algorithm_2_5Command : CommandAsyncBase
{
    public Algorithm_2_5Command()
        : base("algo-2-5")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serverService = new ServerDataService();
        var clientService = new ClientDataService();
        await using var server = await Server.CreateAsync(serverService, cancellationToken);
        await using var client = await Client.CreateAsync(clientService, cancellationToken);
        var tryCount = 0;
        for (var i = 0; i < 10;)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(200).Token, cancellationToken);
            if (await clientService.SendMessageAsync($"message {i}", ++tryCount, cancellationTokenSource.Token) == true)
            {
                i++;
                tryCount = 0;
            }
            if (cancellationToken.IsCancellationRequested == true)
            {
                break;
            }
        };
    }

    public interface IDataService
    {
        [ServerMethod]
        Task SendMessageAsync(string message, CancellationToken cancellationToken);
    }

    public interface IDataCallback
    {
        [ClientMethod]
        void OnMessageSended(string message);
    }

    sealed class ServerDataService : ServerService<IDataService, IDataCallback>, IDataService
    {
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (Random.Shared.Next() % 4 == 0)
            {
                await Console.Out.WriteLineAsync($"server: {message}");
                Client.OnMessageSended(message);
            }
        }
    }

    sealed class ClientDataService : ClientService<IDataService, IDataCallback>, IDataCallback
    {
        private readonly ManualResetEvent _manualResetEvent = new(initialState: false);

        public void OnMessageSended(string message)
        {
            _manualResetEvent.Set();
        }

        public async Task<bool> SendMessageAsync(string message, int tryCount, CancellationToken cancellationToken)
        {
            await Console.Out.WriteLineAsync($"client({tryCount}): {message}");
            _manualResetEvent.Reset();
            await Server.SendMessageAsync(message, cancellationToken);
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (_manualResetEvent.WaitOne(1) == true)
                    return true;
            }
            return false;
        }
    }
}
