using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class NodeClientService : ClientService<INodeService>
{
    public void Request(int r, int c, int n)
    {
        Server.Request(r, c, n);
    }

    public void PrePrepare(int v, int s, int r, int p)
    {
        Server.PrePrepare(v, s, r, p);
    }

    public void Prepare(int v, int s, int r, int b)
    {
        Server.Prepare(v, s, r, b);
    }

    public void Commit(int v, int s, int ni)
    {
        Server.Commit(v, s, ni);
    }

    public void ViewChange(int v, (int s, int r)[] Pb, int b)
    {
        Server.ViewChange(v, Pb, b);
    }

    public void NewView(int v, int p, int ni)
    {
        Server.NewView(v, p, ni);
    }
}
