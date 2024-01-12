using System.Linq;
using System.Net;
using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_12Command
{
    sealed class Client : IAsyncDisposable
    {
        private Common.Client[] _senders = [];
        private ICommandService[] _senderServices = [];
        private int?[] _tickets = [];
        private bool[] _ready = [];
        private string _name = string.Empty;

        public static async Task<Client> CreateAsync(string name, DnsEndPoint[] serverEndPoints, CancellationToken cancellationToken)
        {
            var senders = new Common.Client[serverEndPoints.Length];
            var senderServices = new ClientMessageService[serverEndPoints.Length];
            for (var i = 0; i < serverEndPoints.Length; i++)
            {
                senderServices[i] = new ClientMessageService($"client {i}");
                senders[i] = await Common.Client.CreateAsync(serverEndPoints[i], senderServices[i], cancellationToken);
            }
            return new Client
            {
                _name = name,
                _senders = senders,
                _senderServices = senderServices,
                _tickets = new int?[serverEndPoints.Length],
                _ready = new bool[serverEndPoints.Length],
            };
        }

        public string Name => _name;

        public async ValueTask DisposeAsync()
        {
            for (var i = 0; i < _senders.Length; i++)
            {
                await _senders[i].DisposeAsync();
            }
            _senderServices = [];
            _senders = [];
        }

        public async Task RunAsync(string command, CancellationToken cancellationToken)
        {
            var majority = _senderServices.Length / 2.0;
            while (cancellationToken.IsCancellationRequested == false)
            {
                // step 1
                var tasks1 = new Task<int>[_senderServices.Length];
                for (var i = 0; i < _senderServices.Length; i++)
                {
                    tasks1[i] = _senderServices[i].RequestTicketAsync(cancellationToken);
                }
                await TryWhenAll(tasks1);
                _tickets = [.. tasks1.Select(item => item.IsCompletedSuccessfully == true ? (int?)item.Result : null)];

                // step 2
                if (_tickets.Where(item => item is not null).Count() >= majority)
                {
                    var tasks2 = new Task[_senderServices.Length];
                    for (var i = 0; i < _senderServices.Length; i++)
                    {
                        tasks2[i] = _senderServices[i].SendCommandAsync(_tickets[i]!.Value, command, cancellationToken);
                    }
                    await TryWhenAll(tasks2);
                    _ready = [.. tasks2.Select(item => item.IsCompletedSuccessfully)];
                }
                else
                {
                    continue;
                }

                // step 3
                if (_ready.Where(item => item).Count() >= majority)
                {
                    var tasks3 = new Task[_senderServices.Length];
                    for (var i = 0; i < _senderServices.Length; i++)
                    {
                        tasks3[i] = _senderServices[i].ExecuteCommandAsync(cancellationToken);
                    }
                    await TryWhenAll(tasks3);
                }
                else
                {
                    continue;
                }

                await Task.Delay(1, cancellationToken);
            }
        }

        private static async Task TryWhenAll(Task[] tasks)
        {
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {

            }
        }
    }
}
