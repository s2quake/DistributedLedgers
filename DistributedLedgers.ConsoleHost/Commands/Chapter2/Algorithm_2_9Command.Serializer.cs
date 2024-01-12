using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_9Command
{
    sealed class Serializer : IAsyncDisposable
    {
        private Client[] _senders = [];
        private Server? _receiver;

        public static async Task<Serializer> CreateAsync(int port, int[] serverPorts, CancellationToken cancellationToken)
        {
            var senders = new Client[serverPorts.Length];
            var senderServices = new SerializerSendService[serverPorts.Length];
            for (var i = 0; i < serverPorts.Length; i++)
            {
                senderServices[i] = new SerializerSendService();
                senders[i] = await Client.CreateAsync(serverPorts[i], senderServices[i], cancellationToken);
            }
            var receiver = await Server.CreateAsync(port, new SerializerCallbackService(senderServices), cancellationToken);
            return new Serializer
            {
                _senders = senders,
                _receiver = receiver,
            };
        }

        public async ValueTask DisposeAsync()
        {
            await _receiver!.DisposeAsync();
            _receiver = null;
            for (var i = 0; i < _senders.Length; i++)
            {
                await _senders[i].DisposeAsync();
            }
            _senders = [];
        }
    }

    sealed class SerializerSendService : ClientService<IMessageService>, IMessageService
    {
        public async Task SendMessageAsync(int index, string message, CancellationToken cancellationToken)
        {
            await Server.SendMessageAsync(index, message, cancellationToken);
        }
    }

    sealed class SerializerCallbackService(IMessageService[] dataServices) : ServerService<IMessageService>, IMessageService
    {
        private readonly IMessageService[] _dataServices = dataServices;

        public async Task SendMessageAsync(int index, string message, CancellationToken cancellationToken)
        {
            var tasks = _dataServices.Select(item => item.SendMessageAsync(index, message, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}
