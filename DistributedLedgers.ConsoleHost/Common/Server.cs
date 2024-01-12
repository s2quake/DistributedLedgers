using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class Server : ServerContext, IAsyncDisposable
{
    private Guid _token;

    private Server(IService[] services)
        : base(services)
    {
    }

    public static Task<Server> CreateAsync(IService service, CancellationToken cancellationToken) => CreateAsync(DefaultPort, service, cancellationToken);

    public static async Task<Server> CreateAsync(int port, IService service, CancellationToken cancellationToken)
    {
        var server = new Server([service]) { Port = port };
        server._token = await server.OpenAsync(cancellationToken);
        return server;
    }

    public static async Task<AsyncDisposableCollection<Server>> CreateManyAsync(int[] ports, IService[] services, CancellationToken cancellationToken)
    {
        var tasks = new Task<Server>[ports.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = CreateAsync(ports[i], services[i], cancellationToken);
        }
        return await AsyncDisposableCollection<Server>.CreateAsync(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAsync(_token, CancellationToken.None);
        }
        catch
        {
            await AbortAsync(_token);
        }
    }
}
