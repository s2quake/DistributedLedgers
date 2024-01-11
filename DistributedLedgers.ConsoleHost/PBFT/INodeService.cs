using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

public interface INodeService
{
    [OperationContract]
    Task RequestAsync(int r, int c, int n, CancellationToken cancellationToken);

    [OperationContract]
    Task PrePrepareAsync(int v, int s, int r, int p, CancellationToken cancellationToken);

    [OperationContract]
    Task PrepareAsync(int v, int s, int r, int b, CancellationToken cancellationToken);

    [OperationContract]
    Task CommitAsync(int v, int s, int n, CancellationToken cancellationToken);
}
