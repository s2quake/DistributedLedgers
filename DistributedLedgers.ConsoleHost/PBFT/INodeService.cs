using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

public interface INodeService
{
    [ServerMethod]
    Task RequestAsync(int v, int r, int c, int ni, CancellationToken cancellationToken);

    [ServerMethod(IsOneWay = true)]
    void PrePrepare(int v, int s, int r, int p);

    [ServerMethod(IsOneWay = true)]
    void Prepare(int v, int s, int r, int b);

    [ServerMethod(IsOneWay = true)]
    void Commit(int v, int s, int ni);

    [ServerMethod(IsOneWay = true)]
    void ViewChange(int v1, (int s, int r)[] Pb, int b);

    [ServerMethod(IsOneWay = true)]
    void NewView(int v1, (int s, int r)[] V, (int s, int r)[] O, int p);
}
