using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class NodeServerService(Node node) : ServerServiceHost<INodeService>, INodeService
{
    private readonly Node _node = node;

    public async Task CommitAsync(int v, int s, int n, CancellationToken cancellationToken)
    {
        _node.OnCommit(v, s, n);
        await Task.CompletedTask;
    }

    public async Task PrepareAsync(int v, int s, int r, int n, CancellationToken cancellationToken)
    {
        _node.OnPrepare(v, s, r, n);
        await Task.CompletedTask;
    }

    public async Task PrePrepareAsync(int v, int s, int r, int n, CancellationToken cancellationToken)
    {
        _node.OnPrePrepare(v, s, r, n);
        await Task.CompletedTask;
    }

    public async Task RequestAsync(int r, int c, int n, CancellationToken cancellationToken)
    {
        _node.OnRequest(r, c, n);
        await Task.CompletedTask;
    }
}
