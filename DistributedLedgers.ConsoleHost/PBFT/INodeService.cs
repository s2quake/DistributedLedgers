using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

public interface INodeService
{
    [ServerMethod(IsOneWay = true)]
    void Request(int r, int c, int ni);

    [ServerMethod(IsOneWay = true)]
    void PrePrepare(int v, int s, int r, int p);

    [ServerMethod(IsOneWay = true)]
    void Prepare(int v, int s, int r, int b);

    [ServerMethod(IsOneWay = true)]
    void Commit(int v, int s, int ni);

    [ServerMethod(IsOneWay = true)]
    void ViewChange(int v, (int s, int r)[] Pb, int b);

    [ServerMethod(IsOneWay = true)]
    void NewView(int v, int p, int ni);
}