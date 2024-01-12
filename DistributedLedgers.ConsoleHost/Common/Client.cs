using System.Net;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

sealed class Client : ClientContext, IAsyncDisposable
{
    private Guid _token;

    private Client(IService[] services)
        : base(services)
    {
    }

    public static Task<Client> CreateAsync(IService service, CancellationToken cancellationToken) => CreateAsync(new(DefaultHost, DefaultPort), service, cancellationToken);

    public static async Task<Client> CreateAsync(DnsEndPoint endPoint, IService service, CancellationToken cancellationToken)
    {
        var client = new Client([service])
        {
            EndPoint = endPoint,
        };
        client._token = await client.OpenAsync(cancellationToken);
        return client;
    }

    public static async Task<AsyncDisposableCollection<Client>> CreateManyAsync(DnsEndPoint[] endPoints, IService[] services, CancellationToken cancellationToken)
    {
        var tasks = new Task<Client>[endPoints.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = CreateAsync(endPoints[i], services[i], cancellationToken);
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
