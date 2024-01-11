using System.Collections.Concurrent;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_21Command
{
    public interface INodeService
    {
        [OperationContract]
        Task ProposeAsync(bool value, int round, CancellationToken cancellationToken);
    }

    sealed class NodeServerService : ServerServiceHost<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<int, ConcurrentBag<bool>> _proposesByRound = new();

        async Task INodeService.ProposeAsync(bool value, int round, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
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

    sealed class NodeClientService : ClientServiceHost<INodeService>
    {
        public async void Propose(bool value, int round)
        {
            await Service.ProposeAsync(value, round, CancellationToken.None);
        }
    }

    sealed class Node : NodeBase<Node, NodeServerService, NodeClientService>
    {
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
                var proposes = await ServerService.WaitForProposeAsync(r, n - f, cancellationToken);
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
    }
}
