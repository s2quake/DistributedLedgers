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

    public (int r, int c)[] Value => [.. _replyMessageList.OrderBy(item => item.c)];

    public void Initialize(EndPoint[] endPoints, int f)
    {
        _endPoints = endPoints;
        _f = f;
        _view = new View(v: 0, endPoints, f, this);
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
        }
        _view.Dispatcher.InvokeAsync(() => _view.RequestFromClient(r, c));
    }

    internal async void OnRequest(int r, int c, int ni)
    {
        await _view!.RequestFromBackupAsync(r, c, ni, CancellationToken.None);
    }

    internal void OnPrePrepare(int v, int s, int r, int ni)
    {
        if (_view is { } view && view.Index == v)
        {
            view.Dispatcher.InvokeAsync(() => view.PrePrepare(v, s, r, ni));
        }
    }

    internal void OnPrepare(int v, int s, int r, int ni)
    {
        if (_view is { } view && view.Index == v)
        {
            view.Dispatcher.InvokeAsync(() => view.Prepare(v, s, r, ni));
        }
    }

    internal void OnCommit(int v, int s, int ni)
    {
        if (_view is { } view && view.Index == v)
        {
            view.Dispatcher.InvokeAsync(() => view.Commit(v, s, ni));
        }
    }

    internal void OnViewChange(int v, (int s, int r)[] Pb, int b)
    {
        if (_view != null)
        {
            _view.Dispose();
            _viewByIndex.Add(_view.Index, _view);
        }
        _view = new View(v, _endPoints, _f, this);
        _view.ViewChange(Pb);
    }

    internal void OnNewView(int v, int p, int ni)
    {
        lock (_lockObject)
        {

        }
    }

    internal void BroadcastPrePrepare(int v, int s, int r, int p)
    {
        Broadcast((node, service) =>
        {
            if (node != EndPoint)
            {
                service.PrePrepare(v, s, r, p);
            }
        });
    }

    internal void BroadcastPrepare(int v, int s, int r, int b)
    {
        Broadcast((node, service) =>
        {
            if (node != EndPoint)
            {
                service.Prepare(v, s, r, b);
            }
        });
    }

    internal void BroadcastCommit(int v, int s, int ni)
    {
        Broadcast((node, service) =>
        {
            if (node != EndPoint)
            {
                service.Commit(v, s, ni);
            }
        });
    }

    internal void BroadcastNewView(int v, int p, int ni)
    {
        Broadcast((node, service) =>
        {
            service.NewView(v, p, ni);
        });
    }

    internal void BroadcastViewChange(int v, (int s, int r)[] Pb, int b)
    {
        Broadcast((node, service) =>
        {
            service.ViewChange(v, Pb, b);
        });
    }

    internal void SendRequest(EndPoint endPoint, int r, int c, int ni)
    {
        // var service = GetClientService(receiverNode);
        // await service.Request(r, c, ni, cancellationToken);
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

    protected override async Task<(Client, INodeService)> CreateClientAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        var clientService = new ClientService<INodeService>();
        var client = await Client.CreateAsync(endPoint, clientService, cancellationToken);
        return (client, clientService.Server);
    }

    protected override Task<Server> CreateServerAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        return Server.CreateAsync(endPoint, new ServerService<INodeService>(this), cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view?.Dispose();
        return base.OnDisposeAsync();
    }

    #region INodeService

    void INodeService.Request(int r, int c, int ni) => OnRequest(r, c, ni);

    void INodeService.PrePrepare(int v, int s, int r, int p) => OnPrePrepare(v, s, r, p);

    void INodeService.Prepare(int v, int s, int r, int b) => OnPrepare(v, s, r, b);

    void INodeService.Commit(int v, int s, int ni) => OnCommit(v, s, ni);

    void INodeService.ViewChange(int v, (int s, int r)[] Pb, int b) => OnViewChange(v, Pb, b);

    void INodeService.NewView(int v, int p, int ni) => OnNewView(v, p, ni);

    #endregion
}
