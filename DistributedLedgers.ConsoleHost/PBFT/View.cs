using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class View(int v, int n, int f, Node node)
{
    private readonly int _v = v;
    private readonly int _n = n;
    private readonly int _f = f;
    private readonly int _ni = node.Index;
    private readonly Node _node = node;
    private readonly RequestMessageCollection _requestMessages = [];
    private readonly PrePrepareMessageCollection _prePrepareMessages = [];
    private readonly PrepareMessageCollection _prepareMessages = [];
    private readonly CommitMessageCollection _commitMessages = [];
    private int _s;

    public int Index => _v;

    public override string ToString()
    {
        if (_v == _ni)
            return TerminalStringBuilder.GetString($"{_node}", TerminalColorType.Green);
        return $"{_node}";
    }

    public async void RequestFromClient(int r, int c)
    {
        // Console.WriteLine($"{this} Request: r={r}, c={c}");
        var isPrimary = _v % _n == _ni;
        var ni = _ni;

        var s = Interlocked.Increment(ref _s);
        _requestMessages.Add(r: r, c: c, s: s);
        if (isPrimary == true)
        {
            _node.BroadcastPrePrepare(_v, s, r, p: ni);
        }
        else
        {
            var primaryNode = _node.Nodes.First(item => item.Index == _v);
            try
            {
                await _node.SendRequestAsync(primaryNode, r, c, ni, CancellationToken.None);
            }
            catch (Exception)
            {
                var Pb = _prepareMessages.Collect();
                _node.BroadcastViewChange(_v + 1, Pb, _ni);
            }
        }
    }

    public async Task RequestFromBackupAsync(int r, int c, int n, CancellationToken cancellationToken)
    {
        // Console.WriteLine($"{this} Request from backup: r={r}, c={c}, n={n}");
        await Task.CompletedTask;
    }

    public void PrePrepare(int v, int s, int r, int p)
    {
        // Console.WriteLine($"{this} PrePrepare: v={v}, s={s}, r={r}, p={p}");
        if (_ni == _v)
            throw new InvalidOperationException();

        if (p == (_v % _n) && _prePrepareMessages.Add(v: v, s: s, r: r, p: p) == true)
        {
            _prepareMessages.Add(v: v, s: s, r: r);
            _node.BroadcastPrepare(v, s, r, b: _ni);
        }
    }

    public void Prepare(int v, int s, int r, int ni)
    {
        // Console.WriteLine($"{this} Prepare: v={v}, s={s}, r={r}, n={n}");
        var minimum = 2 * _f;

        if (_prepareMessages.CanCommit(v: v, s: s, r: r, minimum) == true)
        {
            _commitMessages.Add(v: v, s: s, ni: ni);
            _node.BroadcastCommit(v, s, _node.Index);
        }
    }

    public void Commit(int v, int s, int ni)
    {
        // Console.WriteLine($"{this} Commit: v={v}, s={s}, n={n}");
        var minimum = 2 * _f + 1;

        if (_commitMessages.CanReply(v: v, s: s, ni: ni, minimum) == true)
        {
            var items = _requestMessages.Remove(s: s);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                _node.Reply(item.R, item.C);
                Console.WriteLine($"{this} Reply: v={v}, s={item.S}, r={item.R}, c={item.C}");
            }
        }
    }

    public void ViewChange((int s, int r)[] Pb)
    {
        _prepareMessages.AddRange(_v, Pb);
    }
}
