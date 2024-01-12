using System.Collections.Concurrent;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_9Command
{
    public interface INodeService
    {
        [ServerMethod(IsOneWay = true)]
        void SendValue(string nodeId, int value);

        [ServerMethod(IsOneWay = true)]
        void SendMany((string nodeId, int value)[] values);
    }

    sealed class ServerNodeService : ServerService<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<string, int> _valueByNode = new();
        private readonly ConcurrentBag<(string nodeId, int value)[]> _values = [];

        public void SendValue(string nodeId, int value)
        {
            _valueByNode.AddOrUpdate(nodeId, value, (k, v) => value);
        }

        public void SendMany((string nodeId, int value)[] values)
        {
            _values.Add(values);
        }

        public async Task<(string nodeId, int value)[]> WaitForValueAsync(int count, CancellationToken cancellationToken)
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

        public async Task<(string nodeId, int value)[]> WaitForValuesAsync(int count, CancellationToken cancellationToken)
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
}
