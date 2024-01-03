using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class SimpleClient : ClientContextBase, IAsyncDisposable
{
    private Guid _token;

    private SimpleClient(IServiceHost[] serviceHosts)
        : base(serviceHosts)
    {
    }

    public static Task<SimpleClient> CreateAsync(params IServiceHost[] serviceHosts) => CreateAsync(DefaultPort, serviceHosts);

    public static async Task<SimpleClient> CreateAsync(int port, params IServiceHost[] serviceHosts)
    {
        var client = new SimpleClient(serviceHosts) { Port = port };
        client._token = await client.OpenAsync(CancellationToken.None);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(_token, closeCode: 0, CancellationToken.None);
    }
}
