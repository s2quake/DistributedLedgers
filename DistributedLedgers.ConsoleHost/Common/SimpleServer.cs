using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class SimpleServer : ServerContextBase, IAsyncDisposable
{
    private Guid _token;

    private SimpleServer(IServiceHost[] serviceHosts)
        : base(serviceHosts)
    {
    }

    public static Task<SimpleServer> CreateAsync(IServiceHost serviceHost, CancellationToken cancellationToken) => CreateAsync(DefaultPort, serviceHost, cancellationToken);

    public static async Task<SimpleServer> CreateAsync(int port, IServiceHost serviceHost, CancellationToken cancellationToken)
    {
        var server = new SimpleServer([serviceHost]) { Port = port };
        server._token = await server.OpenAsync(cancellationToken);
        Console.WriteLine($"Server:{port} has been created.");
        return server;
    }

    public static async Task<AsyncDisposableCollection<SimpleServer>> CreateManyAsync(int[] ports, IServiceHost[] serviceHosts, CancellationToken cancellationToken)
    {
        var tasks = new Task<SimpleServer>[ports.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = CreateAsync(ports[i], serviceHosts[i], cancellationToken);
        }
        return await AsyncDisposableCollection<SimpleServer>.CreateAsync(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(_token, closeCode: 0, CancellationToken.None);
    }
}
