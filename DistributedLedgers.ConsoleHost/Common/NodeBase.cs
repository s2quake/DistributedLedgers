namespace DistributedLedgers.ConsoleHost;

abstract class NodeBase : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isDisposed;
    private Task? _processTask;

    public void BroadcastMessage(int type, params object[] args)
    {
        ObjectDisposedException.ThrowIf(condition: _isDisposed, this);
        BroadcastService!.Broadcast(this, type, args);
    }

    public void ReceiveMessage(int type, object[] args)
    {
        ObjectDisposedException.ThrowIf(condition: _isDisposed, this);
        OnMessageReceived(type, args);
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(condition: _isDisposed, this);
        _cancellationTokenSource.Cancel();
        OnDispose();
        _isDisposed = true;
    }

    protected abstract Task OnProcess(CancellationToken cancellationToken);

    protected virtual void OnDispose()
    {
    }

    protected virtual void OnMessageReceived(int type, object[] args)
    {
    }

    public bool IsDisposed => _isDisposed;

    internal IBroadcastService? BroadcastService { get; set; }

    async Task ProcessAsync()
    {
        _processTask = OnProcess(_cancellationTokenSource.Token);
    }
}
