using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

partial class Algorithm_2_9Command
{
    public interface IMessageService
    {
        [OperationContract]
        Task SendMessageAsync(string message, CancellationToken cancellationToken);
    }

    sealed class ServerMessageService(string name) : ServerServiceHostBase<IMessageService>, IMessageService
    {
        private readonly string _name = name;

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 2000), cancellationToken);
            await Console.Out.WriteLineAsync($"{_name}: {message}");
        }
    }

    sealed class ClientMessageService(string name) : ClientServiceHostBase<IMessageService>, IMessageService
    {
        private readonly string _name = name;

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            await Service.SendMessageAsync(message, cancellationToken);
            await Console.Out.WriteLineAsync($"{_name}: {message}");
        }
    }
}
