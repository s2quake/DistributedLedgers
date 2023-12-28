using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class SimpleServer : ServerContextBase, IAsyncDisposable
{
    private Guid _token;

    private SimpleServer(IServiceHost[] serviceHosts)
        : base(serviceHosts)
    {
    }

    public static async Task<SimpleServer> CreateAsync(params IServiceHost[] serviceHosts)
    {
        var server = new SimpleServer(serviceHosts);
        server._token = await server.OpenAsync();
        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(_token, closeCode: 0);
    }
}
