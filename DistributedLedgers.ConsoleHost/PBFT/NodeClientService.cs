using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class NodeClientService : ClientService<INodeService>
{
    public async Task RequestAsync(int r, int c, int n, CancellationToken cancellationToken)
    {
        await Server.RequestAsync(r, c, n, cancellationToken);
    }

    public async void PrePrepare(int v, int s, int r, int p)
    {
        await Server.PrePrepareAsync(v, s, r, p, CancellationToken.None);
    }

    public async void Prepare(int v, int s, int r, int b)
    {
        await Server.PrepareAsync(v, s, r, b, CancellationToken.None);
    }

    public async void Commit(int v, int s, int ni)
    {
        await Server.CommitAsync(v, s, ni, CancellationToken.None);
    }

    public async void ViewChange(int v, (int s, int r)[] Pb, int b)
    {
        await Server.ViewChangeAsync(v, Pb, b, CancellationToken.None);
    }

    public async void NewView(int v, int p, int ni)
    {
        await Server.NewViewAsync(v, p, ni, CancellationToken.None);
    }
}
