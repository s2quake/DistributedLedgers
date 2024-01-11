using DistributedLedgers.ConsoleHost.Common;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class Node : NodeBase<Node, NodeServerService, NodeClientService>
{
    private readonly object _lockObject = new();
    private View? _view;
    private readonly List<(int r, int c)> _requestMessageList = [];
    private readonly List<(int r, int c)> _replyMessageList = [];
    private bool _isEnd;

    public (int r, int c)[] Value
    {
        get
        {
            return _replyMessageList.OrderBy(item => item.c).ToArray();
        }
    }

    public void Initialize(int f)
    {
        _view = new View(v: 0, f, this);
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
        _view.RequestFromClient(r, c);
    }

    internal async void OnRequest(int r, int c, int n)
    {
        await _view!.RequestFromBackupAsync(r, c, n, CancellationToken.None);
    }

    internal void OnPrePrepare(int v, int s, int r, int n)
    {
        _view!.PrePrepare(v, s, r, n);
    }

    internal void OnPrepare(int v, int s, int r, int n)
    {
        _view!.Prepare(v, s, r, n);
    }

    internal void OnCommit(int v, int s, int n)
    {
        _view!.Commit(v, s, n);
    }

    internal void BroadcastPrePrepare(int v, int s, int r, int p)
    {
        Broadcast((node, service) =>
        {
            if (node != this)
            {
                service.PrePrepare(v, s, r, p);
            }
        });
    }

    internal void BroadcastPrepare(int v, int s, int r, int b)
    {
        Broadcast((node, service) =>
        {
            if (node != this)
            {
                service.Prepare(v, s, r, b);
            }
        });
    }

    internal void BroadcastCommit(int v, int s, int n)
    {
        Broadcast((node, service) =>
        {
            if (node != this)
            {
                service.Commit(v, s, n);
            }
        });
    }

    internal async Task SendRequestAsync(Node receiverNode, int r, int c, int n, CancellationToken cancellationToken)
    {
        var service = GetClientService(receiverNode);
        await service.RequestAsync(r, c, n, cancellationToken);
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

    protected override NodeServerService CreateServerService()
        => new(this);
}
