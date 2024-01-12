using System.Collections.Concurrent;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter4;

partial class Algorithm_4_9Command
{
    public interface INodeService
    {
        [ServerMethod]
        Task SendAsync(string nodeId, int value, CancellationToken cancellationToken);

        [ServerMethod]
        Task SendManyAsync((string nodeId, int value)[] values, CancellationToken cancellationToken);
    }

    sealed class ServerNodeService : ServerService<INodeService>, INodeService
    {
        private readonly ConcurrentDictionary<string, int> _valueByNode = new();
        private readonly ConcurrentBag<(string nodeId, int value)[]> _values = [];

        public async Task SendAsync(string nodeId, int value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _valueByNode.AddOrUpdate(nodeId, value, (k, v) => value);
        }

        public async Task SendManyAsync((string nodeId, int value)[] values, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
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

    sealed class ClientNodeService : ClientService<INodeService>
    {
        public async void SendValue(string nodeId, int value)
        {
            await Server.SendAsync(nodeId, value, CancellationToken.None);
        }

        public async void SendMany((string nodeId, int value)[] values)
        {
            await Server.SendManyAsync(values, CancellationToken.None);
        }
    }
}
