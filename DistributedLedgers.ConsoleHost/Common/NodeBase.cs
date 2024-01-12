using System.Net;

namespace DistributedLedgers.ConsoleHost.Common;

abstract class NodeBase<T, TService>
    : IAsyncDisposable
    where T : NodeBase<T, TService>
{
    private readonly List<Client> _clientList = [];
    private readonly Dictionary<EndPoint, TService> _clientServiceByEndPoint = [];
    private readonly List<EndPoint> _nodeList = [];
    private Server? _server;
    private int _index = -1;
    private bool _isByzantine;

    public EndPoint EndPoint => _server?.EndPoint ?? throw new InvalidOperationException();

    public int Index => _index;

    public IReadOnlyList<EndPoint> Nodes => _nodeList;

    public bool IsByzantine => _isByzantine;

    public static async Task<T> CreateAsync(int index, bool isByzantine, EndPoint endPoint, CancellationToken cancellationToken)
    {
        var node = (T)Activator.CreateInstance(typeof(T))!;
        var server = await node.CreateServerAsync(endPoint, cancellationToken);
        var (client, clientService) = await node.CreateClientAsync(endPoint, cancellationToken);
        node._server = server;
        node._index = index;
        node._isByzantine = isByzantine;
        node._clientList.Add(client);
        node._clientServiceByEndPoint.Add(endPoint, clientService);
        Console.WriteLine($"{node}: has been created.");
        return node;
    }

    public static Task<AsyncDisposableCollection<T>> CreateManyAsync(EndPoint[] endPoints, CancellationToken cancellationToken)
    {
        return CreateManyAsync(endPoints, byzantineCount: 0, cancellationToken);
    }

    public static async Task<AsyncDisposableCollection<T>> CreateManyAsync(EndPoint[] endPoints, int byzantineCount, CancellationToken cancellationToken)
    {
        var byzantineIndexes = GetByzantineIndexes();
        var creationTasks = Enumerable.Range(0, endPoints.Length).OrderBy(item => Random.Shared.Next()).Select(item => CreateAsync(item, isByzantine: byzantineIndexes.Contains(item), endPoints[item], cancellationToken)).ToArray();
        await Task.WhenAll(creationTasks);
        var nodes = await AsyncDisposableCollection<T>.CreateAsync(creationTasks);
        await Task.WhenAll(nodes.OrderBy(item => item.GetHashCode()).Select(item => AttachNodesAsync(item, endPoints, cancellationToken)));
        return nodes;

        int[] GetByzantineIndexes()
        {
            var portList = endPoints.ToList();
            var indexList = new List<int>(endPoints.Length);
            for (var i = 0; i < byzantineCount; i++)
            {
                var r = Random.Shared.Next(portList.Count);
                portList.RemoveAt(r);
                indexList.Add(r);
            }
            return [.. indexList];
        }
    }

    public async Task AddNodeAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        var (client, clientService) = await CreateClientAsync(endPoint, cancellationToken);
        lock (this)
        {
            _clientList.Add(client);
            _clientServiceByEndPoint.Add(endPoint, clientService);
            _nodeList.Add(endPoint);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        await Parallel.ForEachAsync(_clientList, (item, _) => item.DisposeAsync());
        if (_server != null)
            await _server.DisposeAsync();
        _server = null;
        Console.WriteLine($"{this}: has been destroyed.");
    }

    public TService GetClientService(EndPoint endPoint) => _clientServiceByEndPoint[endPoint];

    public override string ToString()
    {
        var byzantine = _isByzantine == true ? "😡" : "😀";
        return $"{byzantine} Node({_index + 1})";
    }

    protected void Broadcast(Action<EndPoint, TService> action)
    {
        if (IsByzantine == false || Random.Shared.Next() % 2 == 0)
        {
            Parallel.ForEach(_clientServiceByEndPoint, item => action.Invoke(item.Key, item.Value));
        }
    }

    protected void Send(EndPoint endPoint, Action<TService> action)
    {
        action.Invoke(_clientServiceByEndPoint[endPoint]);
    }

    protected abstract Task<Server> CreateServerAsync(EndPoint endPoint, CancellationToken cancellationToken);

    protected abstract Task<(Client, TService)> CreateClientAsync(EndPoint endPoint, CancellationToken cancellationToken);

    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    private static async Task AttachNodesAsync(T node, IEnumerable<EndPoint> endPoints, CancellationToken cancellationToken)
    {
        var others = endPoints.Where(item => item != node.EndPoint);
        await Task.WhenAll(others.Select(item => node.AddNodeAsync(item, cancellationToken)));
        Console.WriteLine($"{node}: is connected to all nodes.");
    }
}
