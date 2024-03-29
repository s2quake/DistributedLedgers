using System.Net;
using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter3;

partial class Algorithm_3_15Command
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

        public EndPoint EndPoint => _server.EndPoint;

        public static async Task<Node> CreateAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var service = new ServerNodeService($"server: {endPoint}");
            var server = await Server.CreateAsync(endPoint, service, cancellationToken);
            return new Node(server, service);
        }

        public async ValueTask AddNodeAsync(EndPoint endPoint, CancellationToken cancellationToken)
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
                    v = Random.Shared.Next() % 2 == 0;
                }
                round++;
                BroadcastMyValue(v, round);
            }
            throw new NotImplementedException();
        }

        private void BroadcastPropose(bool? v, int round)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.BroadcastPropose($"{_server.EndPoint}", v, round));
        }

        private void BroadcastMyValue(bool v, int round)
        {
            Parallel.ForEach(_clientServiceByClient.Values, item => item.BroadcastMyValue($"{_server.EndPoint}", v, round));
        }
    }
}
