using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

partial class Algorithm_2_13Command
{
    sealed class Client : IAsyncDisposable
    {
        private AsyncDisposableCollection<SimpleClient>? _senders;
        private ICommandService[] _senderServices = [];
        private (int store, string? C)?[] _tickets = [];
        private bool[] _ready = [];
        private string _name = string.Empty;

        public static async Task<Client> CreateAsync(string name, int[] serverPorts, CancellationToken cancellationToken)
        {
            var senderServices = new ClientMessageService[serverPorts.Length];
            for (var i = 0; i < serverPorts.Length; i++)
            {
                senderServices[i] = new ClientMessageService($"client {i}");
            }
            var senders = await SimpleClient.CreateManyAsync(serverPorts, senderServices, cancellationToken);
            return new Client
            {
                _name = name,
                _senders = senders,
                _senderServices = senderServices,
                _tickets = new (int store, string? C)?[serverPorts.Length],
                _ready = new bool[serverPorts.Length],
            };
        }

        public static async Task<AsyncDisposableCollection<Client>> CreateManyAsync(int count, int[] serverPorts, CancellationToken cancellationToken)
        {
            var tasks = new Task<Client>[count];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = CreateAsync($"client {i}", serverPorts, cancellationToken);
            }
            return await AsyncDisposableCollection<Client>.CreateAsync(tasks);
        }

        public string Name => _name;

        public async ValueTask DisposeAsync()
        {
            if (_senders != null)
                await _senders.DisposeAsync();
            _senders = null;
            _senderServices = [];
        }

        public async Task RunAsync(string c, CancellationToken cancellationToken)
        {
            try
            {
                var majority = _senderServices.Length / 2.0;
                var origin = c;
                var t = 0;
                while (cancellationToken.IsCancellationRequested == false)
                {
                    // step 1
                    t++;
                    var tasks1 = new Task<(int, string?)?>[_senderServices.Length];
                    for (var i = 0; i < _senderServices.Length; i++)
                    {
                        tasks1[i] = _senderServices[i].RequestTicketAsync(t, cancellationToken);
                    }
                    await TryWhenAll(tasks1);
                    _tickets = [.. tasks1.Select(item => item.Result)];

                    // step 2
                    if (_tickets.Where(item => item is not null).Count() >= majority)
                    {
                        var maxItem = _tickets.Where(item => item is not null).OrderBy(item => item!.Value.store).Last();
                        if (maxItem!.Value.store > t)
                        {
                            c = maxItem!.Value.C!;
                        }

                        var tasks2 = new Task<bool>[_senderServices.Length];
                        for (var i = 0; i < _tickets.Length; i++)
                        {
                            if (_tickets[i] is not null)
                            {
                                tasks2[i] = _senderServices[i].ProposeCommandAsync(t, c, cancellationToken);
                            }
                            else
                            {
                                tasks2[i] = Task<bool>.Run(() => false);
                            }
                        }
                        await TryWhenAll(tasks2);
                        _ready = [.. tasks2.Select(item => item.Result)];
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
                            tasks3[i] = _senderServices[i].ExecuteCommandAsync(c, cancellationToken);
                        }
                        await TryWhenAll(tasks3);
                        return;
                    }

                    await Task.Delay(1, cancellationToken);
                }
            }
            catch
            {
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
