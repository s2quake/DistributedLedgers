using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class NodeClientService : ClientServiceHost<INodeService>
{
    public async Task RequestAsync(int r, int c, int n, CancellationToken cancellationToken)
    {
        await Service.RequestAsync(r, c, n, cancellationToken);
    }

    public async void PrePrepare(int v, int s, int r, int p)
    {
        await Service.PrePrepareAsync(v, s, r, p, CancellationToken.None);
    }

    public async void Prepare(int v, int s, int r, int b)
    {
        await Service.PrepareAsync(v, s, r, b, CancellationToken.None);
    }

    public async void Commit(int v, int s, int n)
    {
        await Service.CommitAsync(v, s, n, CancellationToken.None);
    }
}
