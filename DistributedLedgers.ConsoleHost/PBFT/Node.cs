using System.Net;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class Node : NodeBase<Node, INodeService>, INodeService
{
    private readonly object _lockObject = new();
    private readonly Dictionary<int, View> _viewByIndex = [];
    private readonly Dictionary<int, int> _clientByRequest = [];
    private readonly HashSet<int> _requestSet = [];
    private readonly List<int?> _commitList = [];
    private readonly HashSet<int> _replySet = [];
    private readonly Broadcaster _broadcaster;
    private int _maxS;
    private int _v = -1;
    private bool _isEnd;
    private int _f;
    private EndPoint[] _endPoints = [];
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
                return [.. _commitList.Select(item => (item.Value, _clientByRequest[item.Value]))];
            }
            catch
            {
                throw;
            }
        }
    }

    public void Initialize(EndPoint[] endPoints, int f, int maxS)
    {
        _endPoints = endPoints;
        _f = f;
        _v = 0;
        _maxS = maxS;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_v == -1)
            throw new InvalidOperationException("Node is not initialized.");

        while (cancellationToken.IsCancellationRequested != true && _isEnd != true)
        {
            await Task.Delay(1, cancellationToken);
        }
        var view = _viewByIndex[_v];
        Console.WriteLine($"{view} v={view.Index}| " + TerminalStringBuilder.GetString($"Completed.", TerminalColorType.BrightMagenta));
    }

    public void Request(int r, int c)
    {
        if (_v == -1)
            throw new InvalidOperationException("Node is not initialized.");

        var contains = false;
        lock (_lockObject)
        {
            if (_clientByRequest.ContainsKey(r) == true && _clientByRequest[r] == c)
                throw new ArgumentException("Invalid client", nameof(c));
            if (_clientByRequest.ContainsKey(r) == true)
                return;
            _clientByRequest.Add(r, c);
            _requestSet.Add(r);
            contains = _commitList.Contains(r);

            if (contains != true && GetView(_v) is { } view)
            {
                view.Dispatcher.InvokeAsync(() => view.OnRequest(r, c));
            }
        }
    }

    internal Task SendRequestAsync(EndPoint endPoint, int v, int r, int c, int ni, CancellationToken cancellationToken)
    {
        return SendAsync(endPoint, (service, cancellationToken) => service.RequestAsync(v, r, c, ni, cancellationToken), cancellationToken);
    }

    internal int[] Commit(View view, int s, int r)
    {
        if (view.Index != _v)
            return [];

        lock (_lockObject)
        {
            while (s >= _commitList.Count)
            {
                _commitList.Add(null);
            }
            if (_requestSet.Contains(r) == true)
            {
                _commitList[s] = r;
                _requestSet.Remove(r);
            }
            var rr = new List<int>();
            while (_s < _commitList.Count && _commitList[_s] is { } r1)
            {
                var c1 = _clientByRequest[r1];
                Console.WriteLine($"{view} v={view.Index}| " + TerminalStringBuilder.GetString($"Execute: s={_s}, c={c1}, r={r1}", TerminalColorType.Yellow));
                _replySet.Add(r1);
                rr.Add(r1);
                _s++;
            }
            if (_s == _maxS && _isEnd != true)
            {
                _isEnd = _clientByRequest.Count == _replySet.Count && _replySet.Count == _commitList.Where(item => item is not null).Count() && _commitList.Count == _commitList.Where(item => item is not null).Count();
            }
            return rr.ToArray();
        }
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

    private View ChangeView(int v)
    {
        lock (_lockObject)
        {
            if (_viewByIndex.ContainsKey(v) == false)
            {
                _viewByIndex.Add(v, new(v, _endPoints, _f, this, _broadcaster));
            }
            _v = v;
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
        if (GetView(v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.OnPrePrepare(v, s, r, p));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    async void INodeService.Prepare(int v, int s, int r, int b)
    {
        if (GetView(v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.OnPrepare(v, s, r, b));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    async void INodeService.Commit(int v, int s, int ni)
    {
        if (GetView(v) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.OnCommit(v, s, ni));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    async void INodeService.ViewChange(int v1, (int s, int r)[] Pb, int b)
    {
        if (GetView(v1 - 1) is { } view)
        {
            await view.Dispatcher.InvokeAsync(() => view.OnViewChange(v1, Pb, b));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    async void INodeService.NewView(int v1, (int s, int r)[] V, (int s, int r)[] O, int p)
    {
        if (ChangeView(v1) is { } view)
        {
            var items = NewMethod();
            await view.Dispatcher.InvokeAsync(() =>
            {
                view.OnNewView(v1, V, O, p);
                foreach (var (r, c) in items)
                {
                    view.OnRequest(r: r, c: c);
                }
            });
        }

        (int r, int c)[] NewMethod()
        {
            lock (_lockObject)
            {
                return _requestSet.Select(item => (r: item, c: _clientByRequest[item])).ToArray();
            }
        }
    }

    #endregion
}
