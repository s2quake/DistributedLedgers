using System.Net;
using JSSoft.Communication.Threading;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class View : IDisposable
{
    private readonly int _v;
    private readonly EndPoint[] _endPoints;
    private readonly int _f;
    private readonly int _ni;
    private readonly Node _node;
    private readonly RequestMessageCollection _requestMessages = [];
    private readonly PrePrepareMessageCollection _prePrepareMessages = [];
    private readonly PrepareMessageCollection _prepareMessages = [];
    private readonly CommitMessageCollection _commitMessages = [];
    private readonly EndPoint _primaryEndPoint;
    private readonly Dispatcher _dispatcher;
    private bool _isDisposed;
    private int _s;
    private Timer? _timer;

    public View(int v, EndPoint[] endPoints, int f, Node node)
    {
        _v = v;
        _endPoints = endPoints;
        _f = f;
        _ni = node.Index;
        _node = node;
        _primaryEndPoint = endPoints[v % endPoints.Length];
        _dispatcher = new(this);
    }

    public int Index => _v;

    public Dispatcher Dispatcher => _dispatcher;

    public override string ToString()
    {
        if (_v == _ni)
            return TerminalStringBuilder.GetString($"{_node}", TerminalColorType.Green);
        return $"{_node}";
    }

    public void RequestFromClient(int r, int c)
    {
        _dispatcher.VerifyAccess();
        // Console.WriteLine($"{this} Request: r={r}, c={c}");
        var isPrimary = _v % _endPoints.Length == _ni;
        var ni = _ni;

        var s = Interlocked.Increment(ref _s);
        _requestMessages.Add(r: r, c: c, s: s);
        if (isPrimary == true)
        {
            _node.BroadcastPrePrepare(_v, s, r, p: ni);
        }
        else
        {
            _node.SendRequest(_primaryEndPoint, r, c, ni);
        }
    }

    public async Task RequestFromBackupAsync(int r, int c, int n, CancellationToken cancellationToken)
    {
        // Console.WriteLine($"{this} Request from backup: r={r}, c={c}, n={n}");
        await Task.CompletedTask;
    }

    public void PrePrepare(int v, int s, int r, int p)
    {
        _dispatcher.VerifyAccess();
        // Console.WriteLine($"{this} PrePrepare: v={v}, s={s}, r={r}, p={p}");
        if (_ni == _v)
            throw new InvalidOperationException();

        if (p == (_v % _endPoints.Length) && _prePrepareMessages.Add(v: v, s: s, r: r, p: p) == true)
        {
            _prepareMessages.Add(v: v, s: s, r: r);
            _node.BroadcastPrepare(v, s, r, b: _ni);
        }
    }

    public void Prepare(int v, int s, int r, int ni)
    {
        _dispatcher.VerifyAccess();
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
        _dispatcher.VerifyAccess();
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
        _dispatcher.VerifyAccess();
        _prepareMessages.AddRange(_v, Pb);
    }

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        _dispatcher.Dispose();
        _isDisposed = true;
    }
}
