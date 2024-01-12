using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

public interface INodeService
{
    [ServerMethod]
    Task RequestAsync(int r, int c, int ni, CancellationToken cancellationToken);

    [ServerMethod]
    Task PrePrepareAsync(int v, int s, int r, int p, CancellationToken cancellationToken);

    [ServerMethod]
    Task PrepareAsync(int v, int s, int r, int b, CancellationToken cancellationToken);

    [ServerMethod]
    Task CommitAsync(int v, int s, int ni, CancellationToken cancellationToken);

    [ServerMethod]
    Task ViewChangeAsync(int v, (int s, int r)[] Pb, int b, CancellationToken cancellationToken);

    [ServerMethod]
    Task NewViewAsync(int v, int p, int ni, CancellationToken cancellationToken);
}
