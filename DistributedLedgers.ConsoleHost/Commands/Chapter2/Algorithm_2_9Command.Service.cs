using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_9Command
{
    public interface IMessageService
    {
        [ServerMethod]
        Task SendMessageAsync(int index, string message, CancellationToken cancellationToken);
    }

    sealed class ServerMessageService(string name) : ServerService<IMessageService>, IMessageService
    {
        private readonly string _name = name;
        private readonly Dictionary<int, string> _messageByIndex = [];

        public string[] Messages
        {
            get => _messageByIndex.OrderBy(item => item.Key).Select(item => item.Value).ToArray();
        }

        public async Task SendMessageAsync(int index, string message, CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 2000), cancellationToken);
            // await Console.Out.WriteLineAsync($"{_name}: {message}");
            _messageByIndex[index] = message;
        }
    }

    sealed class ClientMessageService(string name) : ClientService<IMessageService>, IMessageService
    {
        private readonly string _name = name;

        public async Task SendMessageAsync(int index, string message, CancellationToken cancellationToken)
        {
            await Server.SendMessageAsync(index, message, cancellationToken);
            // await Console.Out.WriteLineAsync($"{_name}: {message}");
        }
    }
}
