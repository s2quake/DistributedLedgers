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

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            var x = Random.Shared.Next();
            var n = Nodes.Count + 1;
            var f = n % 3 == 0 ? (n / 3 - 1) : (int)Math.Floor(n / 3.0);
            var nodeIndex = Index;

            for (var i = 0; i <= f; i++)
            {
                // round 1
                Broadcast(item => item.Value(nodeIndex, x));

                // round 2
                var valueByNodeIndex = await ServerService.WaitForValueAsync(cancellationToken);
                var v1 = valueByNodeIndex.ToLookup(item => item.Value)
                            .Where(item => item.Count() >= (n - f))
                            .FirstOrDefault();
                if (v1 != null)
                {
                    Broadcast(item => item.Propose(nodeIndex, v1.Key));
                }

                var proposeByNodeIndex = await ServerService.WaitForProposeAsync(cancellationToken);
                var p1 = proposeByNodeIndex.ToLookup(item => item.Value)
                            .Where(item => item.Count() >= f)
                            .FirstOrDefault();
                if (p1 != null)
                {
                    x = p1.Key;
                }

                // round 3
                if (nodeIndex == i)
                {
                    Broadcast(item => item.Propose(nodeIndex, x));
                }

                var proposeByNodeIndex2 = await ServerService.WaitForProposeAsync(cancellationToken);
                var p2 = proposeByNodeIndex2.Values.Where(item => item == x);
                if (p2.Count() < (n - f) && proposeByNodeIndex2.TryGetValue(i, out int value))
                {
                    x = value;
                }
            }
            Value = x;
        }
    }
}
