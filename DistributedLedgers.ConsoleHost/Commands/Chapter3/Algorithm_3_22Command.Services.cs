using System.Collections.Concurrent;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter3;

partial class Algorithm_3_22Command
{
    public interface INodeService
    {
        [OperationContract]
        Task ProposeAsync(int nodeId, bool? v, int round, CancellationToken cancellationToken);

        [OperationContract]
        Task MyValueAsync(int nodeId, bool v, int round, CancellationToken cancellationToken);

        [OperationContract]
        Task MyCoinAsync(int nodeId, bool c, CancellationToken cancellationToken);

        [OperationContract]
        Task MySetAsync(int nodeId, bool[] cu, CancellationToken cancellationToken);
    }

    sealed class ServerNodeService(string name) : ServerServiceHost<INodeService>, INodeService
    {
        private readonly string _name = name;
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, bool>> _valuesByRound = new();
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, bool?>> _proposesByRound = new();
        private readonly ConcurrentDictionary<int, bool> _coinByNode = new();
        private readonly ConcurrentDictionary<int, bool[]> _setByNode = new();

        public async Task ProposeAsync(int nodeId, bool? v, int round, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (_proposesByRound.GetOrAdd(round, new ConcurrentDictionary<int, bool?>()) is { } proposes)
            {
                proposes.AddOrUpdate(nodeId, v, (key, oldValue) => v);
            }
        }

        public async Task MyValueAsync(int nodeId, bool v, int round, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (_valuesByRound.GetOrAdd(round, new ConcurrentDictionary<int, bool>()) is { } values)
            {
                values.AddOrUpdate(nodeId, v, (key, oldValue) => v);
            }
        }

        public async Task MyCoinAsync(int nodeId, bool c, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _coinByNode.AddOrUpdate(nodeId, c, (key, value) => c);
        }

        public async Task MySetAsync(int nodeId, bool[] cu, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _setByNode.AddOrUpdate(nodeId, cu, (key, value) => cu);
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

        public async Task<bool[]> WaitForCoinAsync(double majority, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_coinByNode.Count > majority)
                {
                    return [.. _coinByNode.Values];
                }
                await Task.Delay(1, cancellationToken);
            }
            throw new NotImplementedException();
        }

        public async Task<bool[]> WaitForSetAsync(double majority, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_setByNode.Count > majority)
                {
                    return [.. _setByNode.Values.SelectMany(item => item)];
                }
                await Task.Delay(1, cancellationToken);
            }
            throw new NotImplementedException();
        }

        public void ClearCoins()
        {
            _setByNode.Clear();
            _coinByNode.Clear();
        }
    }

    sealed class ClientNodeService(string name) : ClientServiceHost<INodeService>
    {
        private readonly string _name = name;

        public async void BroadcastMyValue(int nodeId, bool v, int round)
        {
            await Service.MyValueAsync(nodeId, v, round, CancellationToken.None);
        }

        public async void BroadcastPropose(int nodeId, bool? v, int round)
        {
            await Service.ProposeAsync(nodeId, v, round, CancellationToken.None);
        }

        public async void BroadcastMyCoin(int nodeId, bool c)
        {
            await Service.MyCoinAsync(nodeId, c, CancellationToken.None);
        }

        public async void BroadcastMySet(int nodeId, bool[] cu)
        {
            await Service.MySetAsync(nodeId, cu, CancellationToken.None);
        }
    }
}
