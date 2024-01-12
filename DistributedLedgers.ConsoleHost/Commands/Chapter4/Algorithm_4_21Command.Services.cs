using System.Collections.Concurrent;
using System.Net;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_21Command
{
    public interface INodeService
    {
        [ServerMethod(IsOneWay = true)]
        void Propose(bool value, int round);
    }

    sealed class ServerNodeService : ServerService<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<int, ConcurrentBag<bool>> _proposesByRound = new();

        void INodeService.Propose(bool value, int round)
        {
            var proposes = _proposesByRound.GetOrAdd(round, (r) => []);
            proposes.Add(value);
        }

        public async Task<bool[]> WaitForProposeAsync(int round, int minimumCount, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_proposesByRound.TryGetValue(round, out var proposes) == true && proposes.Count >= minimumCount)
                    break;
                await Task.Delay(1, cancellationToken);
            }
            return [.. _proposesByRound[round]];
        }
    }

    sealed class Node : NodeBase<Node, INodeService>
    {
        private readonly ServerNodeService _serverService = new();

        public bool Value { get; private set; }

        public async Task RunAsync(bool x, int f, CancellationToken cancellationToken)
        {
            var n = Nodes.Count + 1;
            var r = 1;
            var nodeIndex = Index;
            var decided = false;
            Broadcast((_, service) => service.Propose(x, r));
            do
            {
                Console.WriteLine($"{this}: round {r}, value => {x}");
                var proposes = await _serverService.WaitForProposeAsync(r, n - f, cancellationToken);
                var c1 = proposes.ToLookup(item => item).Where(item => item.Count() >= ((n / 2.0) + (3.0 * f) + 1));
                var c2 = proposes.ToLookup(item => item).Where(item => item.Count() >= ((n / 2.0) + f + 1));
                if (c1.Any() == true)
                {
                    x = c1.First().Key;
                    decided = true;
                    Console.WriteLine($"{this}: round {r} decided => {x}");
                }
                else if (c2.Any() == true)
                {
                    x = c2.First().Key;
                }
                else
                {
                    x = Random.Shared.Next() % 2 == 0;
                }
                r++;
                Broadcast((_, service) => service.Propose(x, r));
            } while (decided != true);
            Value = x;
        }

        protected override async Task<(Client, INodeService)> CreateClientAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var clientService = new ClientService<INodeService>();
            var client = await Client.CreateAsync(endPoint, clientService, cancellationToken);
            return (client, clientService.Server);
        }

        protected override Task<Server> CreateServerAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            return Server.CreateAsync(endPoint, _serverService, cancellationToken);
        }
    }
}
