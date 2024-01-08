using System.Collections.Concurrent;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_9Command
{
    public interface INodeService
    {
        [OperationContract]
        Task SendAsync(int nodeId, int value, CancellationToken cancellationToken);

        [OperationContract]
        Task SendManyAsync((int nodeId, int value)[] values, CancellationToken cancellationToken);
    }

    sealed class ServerNodeService(string name) : ServerServiceHost<INodeService>, INodeService
    {
        private readonly string _name = name;
        private readonly ConcurrentDictionary<int, int> _valueByNode = new();
        private readonly ConcurrentBag<(int nodeId, int value)[]> _values = new();

        public async Task SendAsync(int nodeId, int value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _valueByNode.AddOrUpdate(nodeId, value, (k, v) => value);
        }

        public async Task SendManyAsync((int nodeId, int value)[] values, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _values.Add(values);
        }

        public async Task<(int nodeId, int value)[]> WaitForValueAsync(int count, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_valueByNode.Count == count)
                {
                    return _valueByNode.Select(item => (item.Key, item.Value)).ToArray();
                }
                await Task.Delay(1, cancellationToken);
            }
            throw new NotImplementedException();
        }

        public async Task<(int nodeId, int value)[]> WaitForValuesAsync(int count, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested != true)
            {
                if (_values.Count == count)
                {
                    return [.. _values.SelectMany(item => item)];
                }
                await Task.Delay(1, cancellationToken);
            }
            throw new NotImplementedException();
        }
    }

    sealed class ClientNodeService(string name) : ClientServiceHost<INodeService>
    {
        private readonly string _name = name;

        public async void SendValue(int nodeId, int value)
        {
            await Service.SendAsync(nodeId, value, CancellationToken.None);
        }

        public async void SendMany((int nodeId, int value)[] values)
        {
            await Service.SendManyAsync(values, CancellationToken.None);
        }
    }
}
