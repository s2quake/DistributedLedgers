
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;
using JSSoft.Library.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

[Export(typeof(ICommand))]
sealed class Algorithm_2_5Command : CommandAsyncBase
{
    public Algorithm_2_5Command()
        : base("alg-2-5")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serverService = new ServerDataService();
        var clientService = new ClientDataService();
        await using var server = await SimpleServer.CreateAsync(serverService);
        await using var client = await SimpleClient.CreateAsync(clientService);
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
        [OperationContract]
        Task SendMessageAsync(string message, CancellationToken cancellationToken);
    }

    public interface IDataServiceCallback
    {
        [OperationContract]
        void OnMessageSended(string message);
    }

    sealed class ServerDataService : ServerServiceHostBase<IDataService, IDataServiceCallback>, IDataService
    {
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (Random.Shared.Next() % 4 == 0)
            {
                await Console.Out.WriteLineAsync($"server: {message}");
                Callback.OnMessageSended(message);
            }
        }
    }

    sealed class ClientDataService : ClientServiceHostBase<IDataService, IDataServiceCallback>, IDataServiceCallback
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
            await Service.SendMessageAsync(message, cancellationToken);
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (_manualResetEvent.WaitOne(1) == true)
                    return true;
            }
            return false;
        }
    }
}
