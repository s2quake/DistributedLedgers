using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class SimpleServer : ServerContextBase, IAsyncDisposable
{
    private Guid _token;

    private SimpleServer(IServiceHost[] serviceHosts)
        : base(serviceHosts)
    {
    }

    public static Task<SimpleServer> CreateAsync(params IServiceHost[] serviceHosts) => CreateAsync(DefaultPort, serviceHosts);

    public static async Task<SimpleServer> CreateAsync(int port, params IServiceHost[] serviceHosts)
    {
        var server = new SimpleServer(serviceHosts) { Port = port };
        server._token = await server.OpenAsync(CancellationToken.None);
        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(_token, closeCode: 0, CancellationToken.None);
    }
}
