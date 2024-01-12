using JSSoft.Communication;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_12Command
{
    public interface ICommandService
    {
        [ServerMethod]
        Task SendCommandAsync(int ticket, string command, CancellationToken cancellationToken);

        [ServerMethod]
        Task<int> RequestTicketAsync(CancellationToken cancellationToken);

        [ServerMethod]
        Task ExecuteCommandAsync(CancellationToken cancellationToken);
    }

    sealed class ServerMessageService(string name) : ServerService<ICommandService>, ICommandService
    {
        private static readonly object obj = new();
        private readonly string _name = name;
        private string? _command;
        private int _currentTicket;
        private int _ticket;

        public async Task SendCommandAsync(int ticket, string command, CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            lock (obj)
            {
                if (ticket != _currentTicket)
                    throw new ArgumentException("Invalid Ticket", nameof(ticket));
                if (_command != null)
                    Console.WriteLine(TerminalStringBuilder.GetString("Command already exists.", TerminalColorType.Yellow));
                _command = command;
                _currentTicket = 0;
            }
        }

        public async Task<int> RequestTicketAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            lock (obj)
            {
                // if (_currentTicket == _ticket)
                //     throw new InvalidOperationException("Can not create ticket.");
                _ticket++;
                _currentTicket = _ticket;
                return _ticket;
            }
        }

        public async Task ExecuteCommandAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            lock (obj)
            {
                if (_command == null)
                    throw new InvalidOperationException("There is no command to execute.");
                Console.WriteLine($"{_name}: {_command}");
                _command = null;
            }
        }
    }

    sealed class ClientMessageService(string name) : ClientService<ICommandService>, ICommandService
    {
        private readonly string _name = name;
        private int _ticket;

        public async Task SendCommandAsync(int ticket, string command, CancellationToken cancellationToken)
        {
            await Server.SendCommandAsync(ticket, command, cancellationToken);
        }

        public async Task<int> RequestTicketAsync(CancellationToken cancellationToken)
        {
            _ticket = await Server.RequestTicketAsync(cancellationToken);
            return _ticket;
        }

        public async Task ExecuteCommandAsync(CancellationToken cancellationToken)
        {
            await Server.ExecuteCommandAsync(cancellationToken);
        }
    }
}
