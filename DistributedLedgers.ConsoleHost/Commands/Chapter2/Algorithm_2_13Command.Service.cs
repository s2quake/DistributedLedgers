using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

partial class Algorithm_2_13Command
{
    public interface ICommandService
    {
        [OperationContract]
        Task<bool> ProposeCommandAsync(int t, string c, CancellationToken cancellationToken);

        [OperationContract]
        Task<(int store, string? C)?> RequestTicketAsync(int t, CancellationToken cancellationToken);

        [OperationContract]
        Task ExecuteCommandAsync(string c, CancellationToken cancellationToken);
    }

    sealed class ServerMessageService(string name) : ServerServiceHost<ICommandService>, ICommandService
    {
        private static readonly object obj = new();
        private readonly string _name = name;
        private string? _C;
        private int _Tmax;
        private int _Tstore;
        private readonly List<string> _executedList = new();

        public string[] ExecutedCommands => _executedList.ToArray();

        public new string Name => _name;

        public async Task<(int store, string? C)?> RequestTicketAsync(int ticket, CancellationToken cancellationToken)
        {
            // await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            await Task.CompletedTask;
            lock (obj)
            {
                if (ticket > _Tmax)
                {
                    _Tmax = ticket;
                    return (_Tstore, _C);
                }
                return null;
            }
        }

        public async Task<bool> ProposeCommandAsync(int t, string c, CancellationToken cancellationToken)
        {
            // await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            await Task.CompletedTask;
            lock (obj)
            {
                if (t == _Tmax)
                {
                    _C = c;
                    _Tstore = t;
                    return true;
                }
                return false;
            }
        }

        public async Task ExecuteCommandAsync(string c, CancellationToken cancellationToken)
        {
            // await Task.Delay(Random.Shared.Next(100, 1000), cancellationToken);
            await Task.CompletedTask;
            lock (obj)
            {
                Console.WriteLine($"{_name}: {c}");
                _executedList.Add(c);
            }
        }
    }

    sealed class ClientMessageService(string name) : ClientServiceHost<ICommandService>, ICommandService
    {
        private readonly string _name = name;

        public async Task<bool> ProposeCommandAsync(int ticket, string command, CancellationToken cancellationToken)
        {
            return await Service.ProposeCommandAsync(ticket, command, cancellationToken);
        }

        public async Task<(int, string?)?> RequestTicketAsync(int ticket, CancellationToken cancellationToken)
        {
            return await Service.RequestTicketAsync(ticket, cancellationToken);
        }

        public async Task ExecuteCommandAsync(string command, CancellationToken cancellationToken)
        {
            await Service.ExecuteCommandAsync(command, cancellationToken);
        }
    }
}
