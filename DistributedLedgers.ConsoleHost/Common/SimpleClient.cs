using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class SimpleClient : ClientContext, IAsyncDisposable
{
    private Guid _token;

    private SimpleClient(IServiceHost[] serviceHosts)
        : base(serviceHosts)
    {
    }

    public static Task<SimpleClient> CreateAsync(IServiceHost serviceHost, CancellationToken cancellationToken) => CreateAsync(DefaultPort, serviceHost, cancellationToken);

    public static async Task<SimpleClient> CreateAsync(int port, IServiceHost serviceHost, CancellationToken cancellationToken)
    {
        var client = new SimpleClient([serviceHost]) { Port = port };
        client._token = await client.OpenAsync(cancellationToken);
        // Console.WriteLine($"Client: {client} has been created.");
        return client;
    }


    public static async Task<AsyncDisposableCollection<SimpleClient>> CreateManyAsync(int[] ports, IServiceHost[] serviceHosts, CancellationToken cancellationToken)
    {
        var tasks = new Task<SimpleClient>[ports.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = CreateAsync(ports[i], serviceHosts[i], cancellationToken);
        }
        return await AsyncDisposableCollection<SimpleClient>.CreateAsync(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAsync(_token, closeCode: 0, CancellationToken.None);
        }
        catch
        {
            await AbortAsync(_token);
        }
    }
}
