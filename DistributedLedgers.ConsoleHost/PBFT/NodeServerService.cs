using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class NodeServerService(Node node) : ServerService<INodeService>, INodeService
{
    private readonly Node _node = node;

    public async Task RequestAsync(int r, int c, int ni, CancellationToken cancellationToken)
    {
        _node.OnRequest(r, c, ni);
        await Task.CompletedTask;
    }

    public async Task PrePrepareAsync(int v, int s, int r, int ni, CancellationToken cancellationToken)
    {
        _node.OnPrePrepare(v, s, r, ni);
        await Task.CompletedTask;
    }

    public async Task PrepareAsync(int v, int s, int r, int ni, CancellationToken cancellationToken)
    {
        _node.OnPrepare(v, s, r, ni);
        await Task.CompletedTask;
    }

    public async Task CommitAsync(int v, int s, int ni, CancellationToken cancellationToken)
    {
        _node.OnCommit(v, s, ni);
        await Task.CompletedTask;
    }

    public async Task ViewChangeAsync(int v, (int s, int r)[] Pb, int b, CancellationToken cancellationToken)
    {
        _node.OnViewChange(v, Pb, b);
        await Task.CompletedTask;
    }

    public async Task NewViewAsync(int v, int p, int ni, CancellationToken cancellationToken)
    {
        _node.OnNewView(v, p, ni);
        await Task.CompletedTask;
    }
}
