using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class NodeServerService(Node node) : ServerService<INodeService>, INodeService
{
    private readonly Node _node = node;

    public void Request(int r, int c, int ni)
    {
        _node.OnRequest(r, c, ni);
    }

    public void PrePrepare(int v, int s, int r, int ni)
    {
        _node.OnPrePrepare(v, s, r, ni);
    }

    public void Prepare(int v, int s, int r, int ni)
    {
        _node.OnPrepare(v, s, r, ni);
    }

    public void Commit(int v, int s, int ni)
    {
        _node.OnCommit(v, s, ni);
    }

    public void ViewChange(int v, (int s, int r)[] Pb, int b)
    {
        _node.OnViewChange(v, Pb, b);
    }

    public void NewView(int v, int p, int ni)
    {
        _node.OnNewView(v, p, ni);
    }
}
