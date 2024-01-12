using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_9Command
{
    sealed class Node : NodeBase<Node, ServerNodeService, ClientNodeService>
    {
        public int Value { get; private set; }

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            var value = Random.Shared.Next();
            var count = Nodes.Count;

            Broadcast((_, service) => service.SendValue($"{EndPoint}", value));
            var values1 = await ServerService.WaitForValueAsync(count, cancellationToken);

            Broadcast((_, service) => service.SendMany(values1));
            var values2 = await ServerService.WaitForValuesAsync(count, cancellationToken);

            var values3 = values2.Concat(values1);
            var v = values3.Min(item => item.value);
            Value = v;
        }
    }
}
