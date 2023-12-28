using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class SimpleClient : ClientContextBase, IAsyncDisposable
{
    private Guid _token;

    private SimpleClient(IServiceHost[] serviceHosts)
        : base(serviceHosts)
    {
    }

    public static async Task<SimpleClient> CreateAsync(params IServiceHost[] serviceHosts)
    {
        var client = new SimpleClient(serviceHosts);
        client._token = await client.OpenAsync();
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(_token, closeCode: 0);
    }
}
