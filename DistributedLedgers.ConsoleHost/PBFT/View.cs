using System.Net;
using JSSoft.Communication.Threading;
using JSSoft.Terminals;
using IBroadcaster = DistributedLedgers.ConsoleHost.Common.IBroadcaster<DistributedLedgers.ConsoleHost.PBFT.Node, DistributedLedgers.ConsoleHost.PBFT.INodeService>;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class View : IDisposable
{
    private readonly int _v;
    private readonly int _p;
    private readonly EndPoint[] _endPoints;
    private readonly int _f;
    private readonly int _ni;
    private readonly Node _node;
    private readonly IBroadcaster _broadcaster;

    private readonly PrepareMessageCollection _prePrepareMessages = [];
    private readonly PrepareMessageCollection _prepareMessages = [];
    private readonly PrepareMessageCollection _certificateMessages = [];
    private readonly PrepareMessageCollection _viewChangeMessages = [];
    private readonly CommitMessageCollection _commitMessages = [];
    private readonly Dispatcher _dispatcher;
    public bool _isDisposed;
    private int _s;
    private int _s1;

    public View(int v, EndPoint[] endPoints, int f, Node node, IBroadcaster broadcaster)
    {
        _v = v;
        _endPoints = endPoints;
        _f = f;
        _ni = node.Index;
        _p = _endPoints.Length != 0 ? _v % _endPoints.Length : -1;
        _node = node;
        _broadcaster = broadcaster;
        _dispatcher = new(this);
    }

    public int Index => _v;

    public Dispatcher Dispatcher => _dispatcher;

    public bool IsPrimary => _p == _ni;

    public bool IsBackup => _p != _ni;

    public override string ToString()
    {
        if (_v == _ni)
            return TerminalStringBuilder.GetString($"{_node}", TerminalColorType.Green);
        return $"{_node}";
    }

    public void RequestFromClient(int r, int c)
    {
        _dispatcher.VerifyAccess();

        Console.WriteLine($"{this} _v={_v} Request: r={r}");
        var isPrimary = IsPrimary;
        var v = _v;
        var ni = _ni;
        var s = ++_s;
        if (isPrimary == true)
        {
            _broadcaster.SendAll(service => service.PrePrepare(v: v, s: s, r: r, p: ni), predicate: IsNotMe);
        }
        // else
        // {
        //     SendRequestToPrimary(_v, r, c, ni);
        // }
    }

    public void PrePrepare(int v, int s, int r, int p)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (p != _p)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} PrePrepare: v={v}, s={s}, r={r}, p={p}");
        var b = _ni;
        if (IsPrimaryNode(p: p) == true && _prePrepareMessages.Contains(s: s, r: r) == false)
        {
            _prePrepareMessages.Add(s: s, r: r);
            _prepareMessages.Add(s: s, r: r);
            _broadcaster.SendAll(service => service.Prepare(v, s, r, b), predicate: IsNotMe);
        }
    }

    public void Prepare(int v, int s, int r, int b)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (b == _p)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} Prepare: v={v}, s={s}, r={r}, n={b}");
        var minimum = 2 * _f;
        var ni = _ni;
        _prepareMessages.Add(s: s, r: r);
        if (_prepareMessages.CanCommit(s: s, r: r, minimum) == true && _certificateMessages.Contains(s: s, r: r) == false)
        {
            _certificateMessages.Add(s: s, r: r);
            _commitMessages.Add(s: s);
            _broadcaster.SendAll(service => service.Commit(v, s, _node.Index), predicate: IsNotMe);
        }
    }

    public void Commit(int v, int s, int ni)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (ni == _ni)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} Commit: v={v}, s={s}, n={ni}");
        var minimum = 2 * _f + 1;
        if (_commitMessages.CanReply(s: s, minimum) == true)
        {
            var item = _certificateMessages.First(item => item.S == s);
            if (_node.Reply(item.R) == true)
            {
                Console.WriteLine($"{this} Reply: v={v}, s={item.S}, r={item.R}");
            }
        }
    }

    private int _vc;
    public void ViewChange(int v1, (int s, int r)[] Pb, int b)
    {
        _dispatcher.VerifyAccess();
        if (v1 != _v + 1)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} ViewChange: v1={v1}, b={b}");
        _viewChangeMessages.AddRange(Pb);
        _vc++;

        var minimum = 2 * _f + 1;
        if (_vc != minimum)
            return;

        var isPrimary = v1 % _endPoints.Length == _ni;
        if (isPrimary == true)
        {
            var maximumS = _prePrepareMessages.Max(item => item.S);
            var o = _prePrepareMessages.ToLookup(item => item.S);

            for (var i = maximumS; i > 0; i--)
            {
                if (o.Contains(i) == false)
                {
                    _prePrepareMessages.Add(s: i, r: int.MinValue);
                }
            }
            var V = _viewChangeMessages.Collect();
            var O = _prePrepareMessages.Collect();
            var p = _ni;

            _broadcaster.SendAll(item => item.NewView(v1, V, O, p));
        }
    }

    private bool _isNew;
    public void NewView(int v, (int s, int r)[] V, (int s, int r)[] O, int p)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (p != _p)
            throw new InvalidOperationException();
        if (_isNew == true)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} NewView: v1={v}, p={p}");
        var isPrimary = IsPrimary;
        if (V.Length > 0)
        {
            int wqer = 0;
        }
        _certificateMessages.AddRange(O);
        _prePrepareMessages.AddRange(V);
        _isNew = true;
        _s = _prePrepareMessages.Count > 0 ? _prePrepareMessages.Max(item => item.S) : 0;
        // _certificateMessages.SetS(_s);
        _s1 = _s;
        if (_s > 0)
        {
            int qwer = 0;
        }
    }

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        _dispatcher.Dispose();
        _isDisposed = true;
    }

    private async void SendRequestToPrimary(int v, int r, int c, int ni)
    {
        var timeOut = TimeSpan.FromMilliseconds(Random.Shared.Next(200, 500));
        var cancellationTokenSource = new CancellationTokenSource(timeOut);
        try
        {
            var primaryEndPoint = _endPoints[_p];
            await _node.SendRequestAsync(primaryEndPoint, _v, r, c, _ni, cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            await _dispatcher.InvokeAsync(() =>
            {
                var v = _v;
                var Pb = _certificateMessages.Collect();
                var b = _ni;
                Console.WriteLine("Broadcast ViewChange");
                _broadcaster.SendAll(service => service.ViewChange(v + 1, Pb, b));
            });
        }
    }

    private bool IsNotMe(EndPoint endPoint) => endPoint != _node.EndPoint;

    private bool IsPrimaryNode(int p)
    {
        return _v % _endPoints.Length == p;
    }
}
