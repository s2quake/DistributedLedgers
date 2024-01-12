using System.Net;
using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter3;

partial class Algorithm_3_22Command
{
    sealed class Node : IAsyncDisposable
    {
        private readonly List<Client> _clientList = [];
        private readonly Dictionary<Client, ClientNodeService> _clientServiceByClient = [];
        private readonly Server _server;
        private readonly ServerNodeService _serverService;

        private Node(Server server, ServerNodeService serverService)
        {
            _server = server;
            _serverService = serverService;
        }

        public bool? Value { get; private set; }

        public DnsEndPoint EndPoint => _server.EndPoint;

        public static async Task<Node> CreateAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
        {
            var service = new ServerNodeService($"server: {endPoint}");
            var server = await Server.CreateAsync(endPoint, service, cancellationToken);
            return new Node(server, service);
        }

        public async ValueTask AddNodeAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
        {
            var clientService = new ClientNodeService($"client: {endPoint}");
            var client = await Client.CreateAsync(endPoint, clientService, cancellationToken);
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
            Value = null;
            var v = Random.Shared.Next() % 2 == 0;
            var round = 1;
            var decided = false;
            var majority = _clientList.Count / 2.0;
            BroadcastMyValue(v, round);

            while (true)
            {
                var c = await RunSharedCoinAsync(cancellationToken);
                // propose
                var values = await _serverService.WaitForValuesAsync(round, majority, cancellationToken);
                if (values.Distinct().Count() == 1)
                {
                    BroadcastPropose(values.First(), round);
                }
                else
                {
                    BroadcastPropose(v: null, round);
                }

                if (decided == true)
                {
                    BroadcastMyValue(v, round + 1);
                    Value = v;
                    return;
                }

                // apply
                var proposes = await _serverService.WaitForProposesAsync(round, majority, cancellationToken);
                if (proposes.Distinct().Count() == 1 && proposes.First() != null)
                {
                    v = proposes.First()!.Value;
                    decided = true;
                }
                else if (proposes.Where(item => item is not null).Any() == true)
                {
                    v = proposes.Where(item => item is not null).First()!.Value;
                }
                else
                {
                    v = c;
                }
                round++;
                BroadcastMyValue(v, round);
            }
            throw new NotImplementedException();
        }

        private async Task<bool> RunSharedCoinAsync(CancellationToken cancellationToken)
        {
            var nodeCount = _clientList.Count + 1;
            var f = nodeCount / 3.0 - 1;
            var majority = nodeCount - f;
            var c = Random.Shared.Next() % nodeCount != 0;

            // _serverService.ClearCoins();
            BroadcastMyCoin(c);

            var cu = await _serverService.WaitForCoinAsync(majority, cancellationToken);
            BroadcastMySet(cu);
            var coins = await _serverService.WaitForSetAsync(majority, cancellationToken);
            if (coins.Contains(false) == true)
                return false;
            return true;
        }

        private void BroadcastPropose(bool? v, int round)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.BroadcastPropose(_server.EndPoint.Port, v, round));
        }

        private void BroadcastMyValue(bool v, int round)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.BroadcastMyValue(_server.EndPoint.Port, v, round));
        }

        private void BroadcastMyCoin(bool c)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.BroadcastMyCoin(_server.EndPoint.Port, c));
        }

        private void BroadcastMySet(bool[] cu)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.BroadcastMySet(_server.EndPoint.Port, cu));
        }
    }
}
