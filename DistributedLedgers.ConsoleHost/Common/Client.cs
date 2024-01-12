using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class Client : ClientContext, IAsyncDisposable
{
    private Guid _token;

    private Client(IService[] services)
        : base(services)
    {
    }

    public static Task<Client> CreateAsync(IService service, CancellationToken cancellationToken) => CreateAsync(DefaultPort, service, cancellationToken);

    public static async Task<Client> CreateAsync(int port, IService service, CancellationToken cancellationToken)
    {
        var client = new Client([service]) { Port = port };
        client._token = await client.OpenAsync(cancellationToken);
        return client;
    }

    public static async Task<AsyncDisposableCollection<Client>> CreateManyAsync(int[] ports, IService[] services, CancellationToken cancellationToken)
    {
        var tasks = new Task<Client>[ports.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = CreateAsync(ports[i], services[i], cancellationToken);
        }
        return await AsyncDisposableCollection<Client>.CreateAsync(tasks);
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
