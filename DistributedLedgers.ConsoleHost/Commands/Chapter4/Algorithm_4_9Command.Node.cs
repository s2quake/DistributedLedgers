using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_9Command
{
    sealed class Node : IAsyncDisposable
    {
        private readonly List<SimpleClient> _clientList = new();
        private readonly Dictionary<SimpleClient, ClientNodeService> _clientServiceByClient = new();
        private readonly SimpleServer _server;
        private readonly ServerNodeService _serverService;

        private Node(SimpleServer server, ServerNodeService serverService)
        {
            _server = server;
            _serverService = serverService;
        }

        public int Port => _server.Port;

        public int Value { get; private set; }

        public static async Task<Node> CreateAsync(int port, CancellationToken cancellationToken)
        {
            var service = new ServerNodeService($"server: {port}");
            var server = await SimpleServer.CreateAsync(port, service, cancellationToken);
            return new Node(server, service);
        }

        public async ValueTask AddNodeAsync(Node node, CancellationToken cancellationToken)
        {
            var port = node.Port;
            var clientService = new ClientNodeService($"client: {port}");
            var client = await SimpleClient.CreateAsync(port, clientService, cancellationToken);
            _clientList.Add(client);
            _clientServiceByClient.Add(client, clientService);
        }

        public async ValueTask DisposeAsync()
        {
            await Parallel.ForEachAsync(_clientList, (item, _) => item.DisposeAsync());
            await _server.DisposeAsync();
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var value = Random.Shared.Next();
            var count = _clientList.Count;

            BroadcastValue(value);
            var values1 = await _serverService.WaitForValueAsync(count, cancellationToken);

            BroadcastValues(values1);
            var values2 = await _serverService.WaitForValuesAsync(count, cancellationToken);

            var values3 = values2.Concat(values1);
            var v = values3.Min(item => item.value);
            Value = v;
        }

        private void BroadcastValue(int value)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.SendValue(Port, value));
        }

        private void BroadcastValues((int nodeId, int value)[] values)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.SendMany(values));
        }
    }
}
