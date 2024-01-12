using System.Collections.Concurrent;
using System.Net;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_6_2Command
{
    public interface INodeService
    {
        [ServerMethod(IsOneWay = true)]
        void Value(int nodeIndex, bool value);
    }

    sealed class ServerNodeService : ServerService<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<int, bool> _valueByNodeIndex = [];

        void INodeService.Value(int nodeIndex, bool value)
        {
            _valueByNodeIndex.AddOrUpdate(nodeIndex, value, (k, v) => value);
        }

        public async Task<Dictionary<int, bool>> WaitForValueAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return _valueByNodeIndex.ToDictionary();
        }
    }

    sealed class Node : NodeBase<Node, INodeService>
    {
        private readonly ServerNodeService _serverService = new();

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
                var s = await _serverService.WaitForValueAsync(cancellationToken);
                if (s.Count >= i && s.ContainsKey(p) == true && s[p] == true)
                {
                    var b1 = IsByzantine == true ? false : true;
                    Broadcast((_, service) => service.Value(nodeIndex, b1));
                    Value = true;
                    return;
                }
            }
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
