using System.Net;
using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_9Command
{
    sealed class Node : NodeBase<Node, INodeService>
    {
        private readonly ServerNodeService _serverService = new();

        public int Value { get; private set; }

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            var value = Random.Shared.Next();
            var count = Nodes.Count;

            Broadcast((_, service) => service.SendValue($"{EndPoint}", value));
            var values1 = await _serverService.WaitForValueAsync(count, cancellationToken);

            Broadcast((_, service) => service.SendMany(values1));
            var values2 = await _serverService.WaitForValuesAsync(count, cancellationToken);

            var values3 = values2.Concat(values1);
            var v = values3.Min(item => item.value);
            Value = v;
        }

        protected override async Task<(Client, INodeService)> CreateClientAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var clientService = new ClientNodeService();
            var client = await Client.CreateAsync(endPoint, clientService, cancellationToken);
            return (client, clientService.Server);
        }

        protected override Task<Server> CreateServerAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            return Server.CreateAsync(endPoint, _serverService, cancellationToken);
        }
    }
}
