using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

partial class Algorithm_2_10Command
{
    sealed class Client : IAsyncDisposable
    {
        private SimpleClient[] _senders = [];
        private IMessageService[] _senderServices = [];
        private int?[] _locks = [];

        public static async Task<Client> CreateAsync(int[] serverPorts, CancellationToken cancellationToken)
        {
            var senders = new SimpleClient[serverPorts.Length];
            var senderServices = new ClientMessageService[serverPorts.Length];
            for (var i = 0; i < serverPorts.Length; i++)
            {
                senderServices[i] = new ClientMessageService($"client {i}");
                senders[i] = await SimpleClient.CreateAsync(serverPorts[i], senderServices[i], cancellationToken);
            }
            return new Client
            {
                _senders = senders,
                _senderServices = senderServices,
                _locks = new int?[serverPorts.Length],
            };
        }

        public async ValueTask DisposeAsync()
        {
            for (var i = 0; i < _senders.Length; i++)
            {
                await _senders[i].DisposeAsync();
            }
            _senderServices = [];
            _senders = [];
        }

        public async Task RunAsync(string message, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                // step 1
                for (var i = 0; i < _senderServices.Length; i++)
                {
                    try
                    {
                        _locks[i] = await _senderServices[i].LockAsync(cancellationToken);
                    }
                    catch
                    {
                        _locks[i] = null;
                    }
                }

                // step 2
                if (_locks.Where(item => item is not null).Count() == _locks.Length)
                {
                    for (var i = 0; i < _senderServices.Length; i++)
                    {
                        await _senderServices[i].SendMessageAsync(_locks[i]!.Value, message, cancellationToken);
                    }
                    return;
                }
                else
                {
                    for (var i = 0; i < _senderServices.Length; i++)
                    {
                        if (_locks[i] is { } @lock)
                        {
                            await _senderServices[i].UnlockAsync(@lock, cancellationToken);
                        }
                    }
                }
                await Task.Delay(1, cancellationToken);
            }
        }
    }
}
