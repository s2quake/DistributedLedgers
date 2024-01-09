using System.Collections.Concurrent;
using System.Collections.Immutable;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_14Command
{
    public interface INodeService
    {
        [OperationContract]
        Task ValueAsync(int nodeIndex, int value, CancellationToken cancellationToken);

        [OperationContract]
        Task ProposeAsync(int nodeIndex, int value, CancellationToken cancellationToken);
    }

    sealed class ServerNodeService : ServerServiceHost<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<int, int> _valueByNodeIndex = new();
        private readonly ConcurrentDictionary<int, int> _proposeByNodeIndex = new();

        async Task INodeService.ValueAsync(int nodeIndex, int value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _valueByNodeIndex.AddOrUpdate(nodeIndex, value, (k, v) => value);
        }

        async Task INodeService.ProposeAsync(int nodeIndex, int value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _proposeByNodeIndex.AddOrUpdate(nodeIndex, value, (k, v) => value);
        }

        public async Task<ImmutableDictionary<int, int>> WaitForValueAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
            return _valueByNodeIndex.ToImmutableDictionary(item => item.Key, item => item.Value);
        }

        public async Task<ImmutableDictionary<int, int>> WaitForProposeAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
            return _proposeByNodeIndex.ToImmutableDictionary(item => item.Key, item => item.Value);
        }
    }

    sealed class ClientNodeService : ClientServiceHost<INodeService>
    {
        public async void Value(int nodeIndex, int value)
        {
            await Service.ValueAsync(nodeIndex, value, CancellationToken.None);
        }

        public async void Propose(int nodeIndex, int value)
        {
            await Service.ProposeAsync(nodeIndex, value, CancellationToken.None);
        }
    }

    sealed class Node : NodeBase<ServerNodeService, ClientNodeService>
    {
        public int Value { get; private set; }

        public async ValueTask RunAsync(int x, int f, CancellationToken cancellationToken)
        {
            var n = Nodes.Count + 1;
            var nodeIndex = Index;

            for (var i = 1; i <= f + 1; i++)
            {
                // round 1
                var x1 = IsByzantine == true ? NextValue() : x;
                Console.WriteLine($"{this}: step {i}, round 1, value => {x1}");
                Broadcast(item => item.Value(nodeIndex, x1));

                // round 2
                var valueByNodeIndex = await ServerService.WaitForValueAsync(cancellationToken);
                var v1 = valueByNodeIndex.ToLookup(item => item.Value)
                            .Where(item => item.Count() >= (n - f))
                            .FirstOrDefault();
                if (v1 != null)
                {
                    var v2 = IsByzantine == true ? NextValue() : v1.Key;
                    Console.WriteLine($"{this}: step {i}, round 2, propose => {v2}");
                    Broadcast(item => item.Propose(nodeIndex, v2));
                }

                var proposeByNodeIndex = await ServerService.WaitForProposeAsync(cancellationToken);
                var p1 = proposeByNodeIndex.ToLookup(item => item.Value)
                            .OrderByDescending(item => item.Count())
                            .Where(item => item.Count() >= f)
                            .FirstOrDefault();
                if (p1 != null)
                {
                    x = p1.Key;
                    Console.WriteLine($"{this}: step {i}, round 2, set <= {x}");
                }

                // round 3
                if (nodeIndex == i)
                {
                    var x2 = IsByzantine == true ? NextValue() : x;
                    Broadcast(item => item.Propose(nodeIndex, x2));
                    Console.WriteLine($"{this}: step {i}, round 3, 👑, propose => {x2}");
                }

                var proposeByNodeIndex2 = await ServerService.WaitForProposeAsync(cancellationToken);
                var p2 = proposeByNodeIndex2.Values.Where(item => item == x);
                if (p2.Count() < (n - f))
                {
                    if (proposeByNodeIndex2.TryGetValue(i, out var p3) == true)
                    {
                        x = p3;
                        Console.WriteLine($"{this}: step {i}, round 3, set <= {p3}");
                    }
                    else if (i != nodeIndex)
                    {
                        Console.WriteLine($"{this}: step {i}, round 3, king's value does not exist.");
                    }
                }
            }
            Value = x;
        }

        private static int NextValue() => Random.Shared.Next();
    }
}
