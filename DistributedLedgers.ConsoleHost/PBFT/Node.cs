using System.Net;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class Node : NodeBase<Node, INodeService>, INodeService
{
    private readonly object _lockObject = new();
    private readonly Dictionary<int, View> _viewByIndex = [];
    private readonly List<(int r, int c)> _requestMessageList = [];
    private readonly List<(int r, int c)> _replyMessageList = [];
    private View? _view;
    private bool _isEnd;
    private int _f;
    private EndPoint[] _endPoints = [];
    private readonly Broadcaster _broadcaster;

    public Node()
    {
        _broadcaster = new(this);
    }

    public (int r, int c)[] Value => [.. _replyMessageList.OrderBy(item => item.c)];

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
            _requestMessageList.Add((r, c));
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

    internal void Reply(int r, int c)
    {
        lock (_lockObject)
        {
            _requestMessageList.Remove((r, c));
            _replyMessageList.Add((r, c));
            _isEnd = _requestMessageList.Count == 0;
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
        if (_view is { } view && view.Index == v)
        {
            view.Dispatcher.InvokeAsync(() => view.PrePrepare(v, s, r, p));
        }
    }

    void INodeService.Prepare(int v, int s, int r, int b)
    {
        if (_view is { } view && view.Index == v)
        {
            view.Dispatcher.InvokeAsync(() => view.Prepare(v, s, r, b));
        }
    }

    void INodeService.Commit(int v, int s, int ni)
    {
        if (_view is { } view && view.Index == v)
        {
            view.Dispatcher.InvokeAsync(() => view.Commit(v, s, ni));
        }
    }


    private List<(int s, int r)> _V = new();
    void INodeService.ViewChange(int v, (int s, int r)[] Pb1, (int s, int r)[] Pb2, int b)
    {
        lock (_lockObject)
        {
            if (_view is { } view && view.Index != v)
            {
                Console.WriteLine($"view change: {v}");
                _viewByIndex.Add(view.Index, view);
                _view = null;
                // _view = new View(v, _endPoints, _f, this, _broadcaster);
            }

            _V.AddRange(Pb1);
            int qewr = 0;
        }
        // {
        //     if (_view is { } view && view.Index == v)
        //     {
        //         view.Dispatcher.InvokeAsync(() => view.ViewChange(Pb));
        //     }
        // }
    }

    void INodeService.NewView(int v, int p, int ni)
    {

    }

    #endregion
}
