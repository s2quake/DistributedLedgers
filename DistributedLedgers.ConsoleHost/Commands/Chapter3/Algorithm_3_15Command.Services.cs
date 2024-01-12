using System.Collections.Concurrent;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter3;

partial class Algorithm_3_15Command
{
    public interface INodeService
    {
        [ServerMethod]
        Task ProposeAsync(string nodeId, bool? v, int round, CancellationToken cancellationToken);

        [ServerMethod]
        Task MyValueAsync(string nodeId, bool v, int round, CancellationToken cancellationToken);
    }

    sealed class ServerNodeService(string name) : ServerService<INodeService>, INodeService
    {
        private readonly string _name = name;
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, bool>> _valuesByRound = new();
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, bool?>> _proposesByRound = new();

        public async Task ProposeAsync(string nodeId, bool? v, int round, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (_proposesByRound.GetOrAdd(round, new ConcurrentDictionary<string, bool?>()) is { } proposes)
            {
                proposes.AddOrUpdate(nodeId, v, (key, oldValue) => v);
            }
        }

        public async Task MyValueAsync(string nodeId, bool v, int round, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (_valuesByRound.GetOrAdd(round, new ConcurrentDictionary<string, bool>()) is { } values)
            {
                values.AddOrUpdate(nodeId, v, (key, oldValue) => v);
            }
        }

        public async Task<bool[]> WaitForValuesAsync(int round, double majority, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_valuesByRound.TryGetValue(round, out var values) == true && values.Count > majority)
                {
                    return [.. values.Values];
                }
                await Task.Delay(1, cancellationToken);
            }
            throw new NotImplementedException();
        }

        public async Task<bool?[]> WaitForProposesAsync(int round, double majority, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_proposesByRound.TryGetValue(round, out var proposes) == true && proposes.Count > majority)
                {
                    return [.. proposes.Values];
                }
                await Task.Delay(1, cancellationToken);
            }
            throw new NotImplementedException();
        }
    }

    sealed class ClientNodeService(string name) : ClientService<INodeService>
    {
        private readonly string _name = name;

        public async void BroadcastMyValue(string nodeId, bool v, int round)
        {
            await Server.MyValueAsync(nodeId, v, round, CancellationToken.None);
        }

        public async void BroadcastPropose(string nodeId, bool? v, int round)
        {
            await Server.ProposeAsync(nodeId, v, round, CancellationToken.None);
        }
    }
}
