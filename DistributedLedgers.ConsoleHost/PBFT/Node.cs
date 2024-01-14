using System.Net;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class Node : NodeBase<Node, INodeService>, INodeService
{
    private readonly object _lockObject = new();
    private readonly Dictionary<int, View> _viewByIndex = [];
    private readonly Dictionary<int, int> _clientByRequest = [];
    private readonly List<int> _replyList = [];
    private View _view;
    private bool _isEnd;
    private int _f;
    private EndPoint[] _endPoints = [];
    private readonly Broadcaster _broadcaster;

    public Node()
    {
        _broadcaster = new(this);
        _view = new View(v: -1, [], f: 0, this, _broadcaster);
    }

    public (int r, int c)[] Value => [.. _clientByRequest.Select(item => (item.Key, item.Value))];

    public void Initialize(EndPoint[] endPoints, int f)
    {
        _endPoints = endPoints;
        _f = f;
        _view = new View(v: 0, endPoints, f, this, _broadcaster);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_view == null)
            throw new InvalidOperationException("Node is not initialized.");

        while (cancellationToken.IsCancellationRequested != true && _isEnd != true)
        {
            await Task.Delay(1, cancellationToken);
        }
    }

    public void Request(int r, int c)
    {
        if (_view == null)
            throw new InvalidOperationException("Node is not initialized.");

        lock (_lockObject)
        {
            _clientByRequest.Add(r, c);
            if (_view is { } view)
            {
                view.Dispatcher.InvokeAsync(() => view.RequestFromClient(r, c));
            }
        }
    }

    internal Task SendRequestAsync(EndPoint endPoint, int v, int r, int c, int ni, CancellationToken cancellationToken)
    {
        return SendAsync(endPoint, (service, cancellationToken) => service.RequestAsync(v, r, c, ni, cancellationToken), cancellationToken);
    }

    internal void ViewChange(int v)
    {

    }

    internal void Reply(int r)
    {
        lock (_lockObject)
        {
            _clientByRequest.Remove(r);
            _replyList.Add(r);
            _isEnd = _clientByRequest.Count == 0;
        }
    }

    protected override Task<Server> CreateServerAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        return Server.CreateAsync(endPoint, new ServerService<INodeService>(this), cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view?.Dispose();
        foreach (var item in _viewByIndex.Values)
        {
            item.Dispose();
        }
        return base.OnDisposeAsync();
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

    void INodeService.PrePrepare(int v, int s, int r, int p)
    {
        lock (_lockObject)
        {
            if (_view is { } view && view.Index == v)
            {
                view.Dispatcher.InvokeAsync(() => view.PrePrepare(v, s, r, p));
            }
        }
    }

    void INodeService.Prepare(int v, int s, int r, int b)
    {
        lock (_lockObject)
        {
            if (_view is { } view && view.Index == v)
            {
                view.Dispatcher.InvokeAsync(() => view.Prepare(v, s, r, b));
            }
        }
    }

    void INodeService.Commit(int v, int s, int ni)
    {
        lock (_lockObject)
        {
            if (_view is { } view && view.Index == v)
            {
                view.Dispatcher.InvokeAsync(() => view.Commit(v, s, ni));
            }
        }
    }

    void INodeService.ViewChange(int v1, (int s, int r)[] Pb, int b)
    {
        lock (_lockObject)
        {
            if (_view is { } view && view.Index == v1 - 1)
            {
                view.Dispatcher.InvokeAsync(() => view.ViewChange(v1, Pb, b));
            }
        }
    }

    void INodeService.NewView(int v1, (int s, int r)[] V, (int s, int r)[] O, int p)
    {
        lock (_lockObject)
        {
            var items = _clientByRequest.Select(item => (r: item.Key, c: item.Value)).ToArray();
            _viewByIndex.Add(_view.Index, _view);
            _view = new View(v1, _endPoints, _f, this, _broadcaster);
            _view.Dispatcher.InvokeAsync(() =>
            {
                _view.NewView(v1, V, O, p);
                foreach (var (r, c) in items)
                {
                    _view.RequestFromClient(r: r, c: c);
                }
            });
        }

        #endregion
    }
}
