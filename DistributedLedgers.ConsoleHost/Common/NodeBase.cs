using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

abstract class NodeBase<TServerService, TClientService>
    : IAsyncDisposable
    where TServerService : class, IServiceHost
    where TClientService : class, IServiceHost
{
    private readonly List<SimpleClient> _clientList = [];
    private readonly Dictionary<SimpleClient, TClientService> _clientServiceByClient = [];
    private readonly List<NodeBase<TServerService, TClientService>> _nodeList = [];
    private SimpleServer? _server;
    private TServerService? _serverService;
    private int _index = -1;

    public int Port => _server?.Port ?? throw new InvalidOperationException();

    public int Index => _index;

    public IReadOnlyList<NodeBase<TServerService, TClientService>> Nodes => _nodeList;

    public static async Task<T> CreateAsync<T>(int index, int port, CancellationToken cancellationToken)
        where T : NodeBase<TServerService, TClientService>, new()
    {
        var node = (T)Activator.CreateInstance(typeof(T))!;
        var serverService = node.CreateServerService();
        var server = await SimpleServer.CreateAsync(port, serverService, cancellationToken);
        node._server = server;
        node._serverService = serverService;
        node._index = index;
        return node;
    }

    public static async Task<AsyncDisposableCollection<T>> CreateManyAsync<T>(int[] ports, CancellationToken cancellationToken)
        where T : NodeBase<TServerService, TClientService>, new()
    {
        var creationTasks = Enumerable.Range(0, ports.Length).Select(item => CreateAsync<T>(item, ports[item], cancellationToken)).ToArray();
        await Task.WhenAll(creationTasks);
        var nodes = await AsyncDisposableCollection<T>.CreateAsync(creationTasks);
        await Parallel.ForEachAsync(nodes, cancellationToken, (item, cancellationToken) => AttachNodesAsync(item, nodes, cancellationToken));
        return nodes;
    }

    public async ValueTask AddNodeAsync(NodeBase<TServerService, TClientService> node, CancellationToken cancellationToken)
    {
        var port = node.Port;
        var clientService = CreateClientService();
        var client = await SimpleClient.CreateAsync(port, clientService, cancellationToken);
        _clientList.Add(client);
        _clientServiceByClient.Add(client, clientService);
        _nodeList.Add(node);
    }

    public async ValueTask DisposeAsync()
    {
        await Parallel.ForEachAsync(_clientList, (item, _) => item.DisposeAsync());
        if (_server != null)
            await _server.DisposeAsync();
        _server = null;
        _serverService = null;
    }

    protected TServerService ServerService => _serverService ?? throw new InvalidOperationException();

    protected void Broadcast(Action<TClientService> action)
    {
        Parallel.ForEach(_clientServiceByClient.Values, action.Invoke);
    }

    protected virtual TServerService CreateServerService()
        => (TServerService)Activator.CreateInstance(typeof(TServerService))!;

    protected virtual TClientService CreateClientService()
        => (TClientService)Activator.CreateInstance(typeof(TClientService))!;

    private static async ValueTask AttachNodesAsync(NodeBase<TServerService, TClientService> node, IEnumerable<NodeBase<TServerService, TClientService>> nodes, CancellationToken cancellationToken)
    {
        var others = nodes.Where(item => item != node);
        await Parallel.ForEachAsync(others, cancellationToken, node.AddNodeAsync);
    }
}