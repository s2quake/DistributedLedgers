using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_10Command
{
    public interface IMessageService
    {
        [OperationContract]
        Task SendMessageAsync(int @lock, string message, CancellationToken cancellationToken);

        [OperationContract]
        Task<int> LockAsync(CancellationToken cancellationToken);

        [OperationContract]
        Task UnlockAsync(int @lock, CancellationToken cancellationToken);
    }

    sealed class ServerMessageService(string name) : ServerServiceHost<IMessageService>, IMessageService
    {
        private static readonly object obj = new();
        private readonly string _name = name;
        private readonly List<string> _messageList = [];
        private int? _lock;

        public async Task SendMessageAsync(int @lock, string message, CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            lock (obj)
            {
                if (_lock != @lock)
                    throw new ArgumentException("Invalid Lock", nameof(@lock));
                _messageList.Add(message);
                _lock = null;
                Console.WriteLine($"{_name}: {message}");
            }
        }

        public async Task<int> LockAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            lock (obj)
            {
                if (_lock != null)
                    throw new InvalidOperationException();
                _lock = Random.Shared.Next();
                return _lock.Value;
            }
        }

        public async Task UnlockAsync(int @lock, CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            lock (obj)
            {
                if (_lock == null)
                    throw new InvalidOperationException();
                if (_lock != @lock)
                    throw new ArgumentException("Invalid Lock", nameof(@lock));
                _lock = null;
            };
        }
    }

    sealed class ClientMessageService(string name) : ClientServiceHost<IMessageService>, IMessageService
    {
        private readonly string _name = name;
        private int? _lock;

        public async Task SendMessageAsync(int @lock, string message, CancellationToken cancellationToken)
        {
            await Service.SendMessageAsync(@lock, message, cancellationToken);
        }

        public async Task<int> LockAsync(CancellationToken cancellationToken)
        {
            _lock = await Service.LockAsync(cancellationToken);
            return _lock.Value;
        }

        public async Task UnlockAsync(int @lock, CancellationToken cancellationToken)
        {
            await Service.UnlockAsync(@lock, cancellationToken);
        }
    }
}
