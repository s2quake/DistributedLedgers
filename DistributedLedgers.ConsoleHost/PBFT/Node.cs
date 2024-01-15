using System.Net;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class Node : NodeBase<Node, INodeService>, INodeService
{
    private readonly object _lockObject = new();
    private readonly Dictionary<int, View> _viewByIndex = [];
    private readonly Dictionary<int, int> _clientByRequest2 = [];
    private readonly Dictionary<int, int> _clientByRequest = [];
    private readonly List<(int r, int c)?> _replyList = [];
    private int _v = -1;
    private bool _isEnd;
    private int _f;
    private EndPoint[] _endPoints = [];
    private readonly Broadcaster _broadcaster;
    private int _s;

    public Node()
    {
        _broadcaster = new(this);
    }

    public (int r, int c)[] Value
    {
        get
        {
            try
            {
                return [.. _replyList.Select(item => (item!.Value.r, item!.Value.c))];
            }
            catch
            {
                foreach (var item in _replyList)
                {
                    Console.WriteLine($"{item}");
                }
                throw;
            }
        }
    }

    public void Initialize(EndPoint[] endPoints, int f)
    {
        _endPoints = endPoints;
        _f = f;
        _v = 0;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_v == -1)
            throw new InvalidOperationException("Node is not initialized.");

        while (cancellationToken.IsCancellationRequested != true && _isEnd != true)
        {
            await Task.Delay(1, cancellationToken);
        }
    }

    public async void Request(int r, int c)
    {
        if (_v == -1)
            throw new InvalidOperationException("Node is not initialized.");

        if (GetView(_v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() =>
            {
                _clientByRequest2.Add(r, c);
                _clientByRequest.Add(r, c);
                _replyList.Add(null);
                view.RequestFromClient(r, c);
            });
        }
    }

    internal Task SendRequestAsync(EndPoint endPoint, int v, int r, int c, int ni, CancellationToken cancellationToken)
    {
        return SendAsync(endPoint, (service, cancellationToken) => service.RequestAsync(v, r, c, ni, cancellationToken), cancellationToken);
    }

    internal int[] Reply(View view, int s, int r)
    {
        if (_clientByRequest.ContainsKey(r) == true)
        {
            while (s > _replyList.Count)
            {
                _replyList.Add(null);
            }
            _replyList[s] = (r, _clientByRequest[r]);
            _clientByRequest.Remove(r);
        }
        var rr = new List<int>();
        while (_s < _replyList.Count && _replyList[_s] is { } item)
        {
            Console.WriteLine($"{view}" + TerminalStringBuilder.GetString($"Reply: v={view.Index}, s={_s}, c={item.c}, r={item.r}", TerminalColorType.Yellow));
            rr.Add(item.r);
            _s++;
        }
        if (_s > s)
        {
            _isEnd = _clientByRequest2.Count == _replyList.Count && _replyList.Any(item => item is null) != true;
        }
        return rr.ToArray();
    }

    protected override Task<Server> CreateServerAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        return Server.CreateAsync(endPoint, new ServerService<INodeService>(this), cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        foreach (var item in _viewByIndex.Values)
        {
            item.Dispose();
        }
        return base.OnDisposeAsync();
    }

    private View GetView(int v)
    {
        lock (_lockObject)
        {
            if (_viewByIndex.ContainsKey(v) == false)
            {
                _viewByIndex.Add(v, new(v, _endPoints, _f, this, _broadcaster));
            }
            return _viewByIndex[v];
        }
    }

    #region Broadcaster

    sealed class Broadcaster(Node node) : IBroadcaster<Node, INodeService>
    {
        private readonly Node _node = node;

        public void Send(EndPoint endPoint, Action<INodeService> action)
        {
            _node.Send(endPoint, action);
        }

        public void SendAll(Action<INodeService> action, Predicate<EndPoint> predicate)
        {
            _node.SendAll(action, predicate);
        }
    }

    #endregion

    #region INodeService

    async Task INodeService.RequestAsync(int v, int r, int c, int ni, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(500, 2000), cancellationToken);
    }

    async void INodeService.PrePrepare(int v, int s, int r, int p)
    {
        // Console.WriteLine($"{this}: INodeService.PrePrepare 1");
        if (GetView(v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.PrePrepare(v, s, r, p));
        }
        else
        {
            throw new NotImplementedException();
        }
        // Console.WriteLine($"{this}: INodeService.PrePrepare 2");
    }

    async void INodeService.Prepare(int v, int s, int r, int b)
    {
        // Console.WriteLine($"{this}: INodeService.Prepare 1");
        if (GetView(v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.Prepare(v, s, r, b));
        }
        else
        {
            throw new NotImplementedException();
        }
        // Console.WriteLine($"{this}: INodeService.Prepare 2");
    }

    async void INodeService.Commit(int v, int s, int ni)
    {
        // Console.WriteLine($"{this}: INodeService.Commit 1");
        if (GetView(v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.Commit(v, s, ni));
        }
        else
        {
            throw new NotImplementedException();
        }
        // Console.WriteLine($"{this}: INodeService.Commit 2");
    }

    async void INodeService.ViewChange(int v1, (int s, int r)[] Pb, int b)
    {
        // Console.WriteLine($"{this}: INodeService.ViewChange 1");
        if (GetView(v1 - 1) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.ViewChange(v1, Pb, b));
        }
        else
        {
            throw new NotImplementedException();
        }
        // Console.WriteLine($"{this}: INodeService.ViewChange 2");
    }

    async void INodeService.NewView(int v1, (int s, int r)[] V, (int s, int r)[] O, int p)
    {
        if (GetView(v1) is { } view)
        {
            var items = _clientByRequest.Select(item => (r: item.Key, c: item.Value)).ToArray();
            _v = v1;
            await view.Dispatcher.InvokeAsync(() =>
            {
                view.NewView(v1, V, O, p);
                foreach (var (r, c) in items)
                {
                    view.RequestFromClient(r: r, c: c);
                }
            });
        }
    }

    #endregion
}
