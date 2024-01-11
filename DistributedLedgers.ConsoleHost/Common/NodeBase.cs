using DistributedLedgers.ConsoleHost.PBFT;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

abstract class NodeBase<T, TServerService, TClientService>
    : IAsyncDisposable
    where T : NodeBase<T, TServerService, TClientService>
    where TServerService : class, IServiceHost
    where TClientService : class, IServiceHost
{
    private readonly List<SimpleClient> _clientList = [];
    private readonly Dictionary<T, TClientService> _clientServiceByNode = [];
    private readonly List<T> _nodeList = [];
    private SimpleServer? _server;
    private TServerService? _serverService;
    private int _index = -1;
    private bool _isByzantine;

    public int Port => _server?.Port ?? throw new InvalidOperationException();

    public int Index => _index;

    public IReadOnlyList<T> Nodes => _nodeList;

    public bool IsByzantine => _isByzantine;

    public static async Task<T> CreateAsync(int index, bool isByzantine, int port, CancellationToken cancellationToken)
    {
        var node = (T)Activator.CreateInstance(typeof(T))!;
        var serverService = node.CreateServerService();
        var clientService = node.CreateClientService();
        var server = await SimpleServer.CreateAsync(port, serverService, cancellationToken);
        var client = await SimpleClient.CreateAsync(port, clientService, cancellationToken);
        node._server = server;
        node._serverService = serverService;
        node._index = index;
        node._isByzantine = isByzantine;
        node._clientList.Add(client);
        node._clientServiceByNode.Add(node, clientService);
        Console.WriteLine($"{node}: has been created.");
        return node;
    }

    public static Task<AsyncDisposableCollection<T>> CreateManyAsync(int[] ports, CancellationToken cancellationToken)
    {
        return CreateManyAsync(ports, byzantineCount: 0, cancellationToken);
    }

    public static async Task<AsyncDisposableCollection<T>> CreateManyAsync(int[] ports, int byzantineCount, CancellationToken cancellationToken)
    {
        var byzantineIndexes = GetByzantineIndexes();
        var creationTasks = Enumerable.Range(0, ports.Length).OrderBy(item => Random.Shared.Next()).Select(item => CreateAsync(item, isByzantine: byzantineIndexes.Contains(item), ports[item], cancellationToken)).ToArray();
        await Task.WhenAll(creationTasks);
        var nodes = await AsyncDisposableCollection<T>.CreateAsync(creationTasks);
        await Task.WhenAll(nodes.OrderBy(item => item.GetHashCode()).Select(item => AttachNodesAsync(item, nodes, cancellationToken)));
        return nodes;

        int[] GetByzantineIndexes()
        {
            var portList = ports.ToList();
            var indexList = new List<int>(ports.Length);
            for (var i = 0; i < byzantineCount; i++)
            {
                var r = Random.Shared.Next(portList.Count);
                portList.RemoveAt(r);
                indexList.Add(r);
            }
            return [.. indexList];
        }
    }

    public async Task AddNodeAsync(T node, CancellationToken cancellationToken)
    {
        var port = node.Port;
        var clientService = CreateClientService();
        var client = await SimpleClient.CreateAsync(port, clientService, cancellationToken);
        lock (this)
        {
            _clientList.Add(client);
            _clientServiceByNode.Add(node, clientService);
            _nodeList.Add(node);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Parallel.ForEachAsync(_clientList, (item, _) => item.DisposeAsync());
        if (_server != null)
            await _server.DisposeAsync();
        _server = null;
        _serverService = null;
        Console.WriteLine($"{this}: has been destroyed.");
    }

    public override string ToString()
    {
        var byzantine = _isByzantine == true ? "😡" : "😀";
        return $"{byzantine} Node({_index + 1})";
    }

    protected TServerService ServerService => _serverService ?? throw new InvalidOperationException();

    protected void Broadcast(Action<T, TClientService> action)
    {
        if (IsByzantine == false || Random.Shared.Next() % 2 == 0)
        {
            Parallel.ForEach(_clientServiceByNode, item => action.Invoke(item.Key, item.Value));
        }
    }

    protected void Send(T receiverNode, Action<TClientService> action)
    {
        action.Invoke(_clientServiceByNode[receiverNode]);
    }

    protected TClientService GetClientService(T node) => _clientServiceByNode[node];

    protected virtual TServerService CreateServerService()
        => (TServerService)Activator.CreateInstance(typeof(TServerService))!;

    protected virtual TClientService CreateClientService()
        => (TClientService)Activator.CreateInstance(typeof(TClientService))!;

    private static async Task AttachNodesAsync(T node, IEnumerable<T> nodes, CancellationToken cancellationToken)
    {
        var others = nodes.Where(item => item != node);
        await Task.WhenAll(others.Select(item => node.AddNodeAsync(item, cancellationToken)));
        Console.WriteLine($"{node}: is connected to all nodes.");
    }
}