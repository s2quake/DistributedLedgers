using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_13Command
{
    public interface ICommandService
    {
        [ServerMethod]
        Task<bool> ProposeCommandAsync(int t, string c, CancellationToken cancellationToken);

        [ServerMethod]
        Task<(int store, string? C)?> RequestTicketAsync(int t, CancellationToken cancellationToken);

        [ServerMethod]
        Task ExecuteCommandAsync(string c, CancellationToken cancellationToken);
    }

    sealed class ServerMessageService(string name) : ServerService<ICommandService>, ICommandService
    {
        private static readonly object obj = new();
        private readonly string _name = name;
        private string? _C;
        private int _max;
        private int _store;
        private readonly List<string> _executedList = [];

        public string[] ExecutedCommands => _executedList.ToArray();

        public new string Name => _name;

        public async Task<(int store, string? C)?> RequestTicketAsync(int ticket, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            lock (obj)
            {
                if (ticket > _max)
                {
                    _max = ticket;
                    return (_store, _C);
                }
                return null;
            }
        }

        public async Task<bool> ProposeCommandAsync(int t, string c, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            lock (obj)
            {
                if (t == _max)
                {
                    _C = c;
                    _store = t;
                    return true;
                }
                return false;
            }
        }

        public async Task ExecuteCommandAsync(string c, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            lock (obj)
            {
                Console.WriteLine($"{_name}: {c}");
                _executedList.Add(c);
            }
        }
    }

    sealed class ClientMessageService(string name) : ClientService<ICommandService>, ICommandService
    {
        private readonly string _name = name;

        public async Task<bool> ProposeCommandAsync(int ticket, string command, CancellationToken cancellationToken)
        {
            return await Server.ProposeCommandAsync(ticket, command, cancellationToken);
        }

        public async Task<(int, string?)?> RequestTicketAsync(int ticket, CancellationToken cancellationToken)
        {
            return await Server.RequestTicketAsync(ticket, cancellationToken);
        }

        public async Task ExecuteCommandAsync(string command, CancellationToken cancellationToken)
        {
            await Server.ExecuteCommandAsync(command, cancellationToken);
        }
    }
}
