using System.Collections.Concurrent;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_6_2Command
{
    public interface INodeService
    {
        [OperationContract]
        Task ValueAsync(int nodeIndex, bool value, CancellationToken cancellationToken);
    }

    sealed class ServerNodeService : ServerServiceHost<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<int, bool> _valueByNodeIndex = [];

        async Task INodeService.ValueAsync(int nodeIndex, bool value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _valueByNodeIndex.AddOrUpdate(nodeIndex, value, (k, v) => value);
        }

        public async Task<Dictionary<int, bool>> WaitForValueAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return _valueByNodeIndex.ToDictionary();
        }
    }

    sealed class ClientNodeService : ClientServiceHost<INodeService>
    {
        public async void Value(int nodeIndex, bool value)
        {
            await Service.ValueAsync(nodeIndex, value, CancellationToken.None);
        }
    }

    sealed class Node : NodeBase<Node, ServerNodeService, ClientNodeService>
    {
        public bool Value { get; private set; }

        public async Task RunAsync(int p, int f, CancellationToken cancellationToken)
        {
            var nodeIndex = Index;
            if (p == nodeIndex)
            {
                await RunPrimaryAsync(cancellationToken);
                return;
            }

            for (var i = 1; i <= f + 1; i++)
            {
                var s = await ServerService.WaitForValueAsync(cancellationToken);
                if (s.Count >= i && s.ContainsKey(p) == true && s[p] == true)
                {
                    var b1 = IsByzantine == true ? false : true;
                    Broadcast((_, service) => service.Value(nodeIndex, b1));
                    Value = true;
                    return;
                }
            }
        }

        private async Task RunPrimaryAsync(CancellationToken cancellationToken)
        {
            var b = Random.Shared.Next() % 2 == 1;
            var nodeIndex = Index;
            if (b == true)
            {
                var b1 = IsByzantine != true || !b;
                Broadcast((_, service) => service.Value(nodeIndex, b1));
                Value = true;
            }
            else
            {
                Value = false;
            }
            await Task.Delay(1, cancellationToken);
        }
    }
}
