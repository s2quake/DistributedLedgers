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
    private bool _isDisposed;
    private int _s;
    private readonly Dictionary<int, Timer> _timerByR = new();
    private bool _isFaulted;
    private bool _isNew;
    private int _vc;

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
            return TerminalStringBuilder.GetString($"p{_node}", TerminalColorType.Green);
        return $" {_node}";
    }

    public void OnRequest(int r, int c)
    {
        _dispatcher.VerifyAccess();

        Console.WriteLine($"{this} v={_v}| " + TerminalStringBuilder.GetString($"Request: r={r}", TerminalColorType.Blue));
        var isPrimary = IsPrimary;
        var v = _v;
        var ni = _ni;
        var s = _s;
        if (isPrimary == true)
        {
            if (_node.IsByzantine != true)
            {
                Console.WriteLine($"{this} v={v}| Broadcast PrePrepare: s={s}, r={r}, p={ni}");
                _broadcaster.SendAll(service => service.PrePrepare(v: v, s: s, r: r, p: ni), predicate: IsNotMe);
            }
            _timerByR.Add(r, new Timer(Time_TimerCallback, r, 1000, int.MaxValue));
            _s++;
        }
        else if (_certificateMessages.FirstOrDefault(item => item.R == r) is { } rr && _commitMessages.FirstOrDefault(item => item.S == rr.S) is { } cm)
        {
            OnCommit(_v, rr.S, _ni);
        }
        else
        {
            _timerByR.Add(r, new Timer(Time_TimerCallback, r, 1000, int.MaxValue));
        }
    }

    private async void Time_TimerCallback(object? state)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            var r = (int)state!;
            if (_isFaulted != true && _timerByR.ContainsKey(r) == true)
            {
                _timerByR[r].Dispose();
                var v = _v;
                var Pb = _certificateMessages.Collect();
                var b = _ni;
                Console.WriteLine($"{this} v={_v}| Broadcast ViewChange: r={state}");
                _isFaulted = true;
                _broadcaster.SendAll(service => service.ViewChange(v + 1, Pb, b));
            }
        });
    }

    public void OnPrePrepare(int v, int s, int r, int p)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (p != _p)
            throw new InvalidOperationException();
        if (IsPrimary == true)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} v={v}| OnPrePrepare: s={s}, r={r}, p={p}");
        if (_prePrepareMessages.Contains(s: s, r: r) == false)
        {
            var b = _ni;
            _prePrepareMessages.Add(s: s, r: r);
            if (_node.IsByzantine != true)
            {
                Console.WriteLine($"{this} v={v}| Broadcast Prepare: s={s}, r={r}, b={b}");
                _broadcaster.SendAll(service => service.Prepare(v, s, r, b));
            }
        }
    }

    public void OnPrepare(int v, int s, int r, int b)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();

        var minimum = 2 * _f;
        var ni = _ni;
        _prepareMessages.Add(s: s, r: r);
        Console.WriteLine($"{this} v={v}| OnPrepare: s={s}, r={r}, b={b}");
        if (_prepareMessages.CanCommit(s: s, r: r, minimum) == true && _certificateMessages.Contains(s: s, r: r) == false)
        {
            _certificateMessages.Add(s: s, r: r);
            if (_node.IsByzantine != true)
            {
                Console.WriteLine($"{this} v={v}| Broadcast Commit: s={s}, ni={ni}");
                _broadcaster.SendAll(service => service.Commit(v, s, ni));
            }
        }
    }

    public void OnCommit(int v, int s, int ni)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();

        var minimum = 2 * _f + 1;
        Console.WriteLine($"{this} v={v}| OnCommit: s={s}, ni={ni}");
        if (_commitMessages.CanReply(s: s, minimum) == true)
        {
            var item = _certificateMessages.First(item => item.S == s);
            Console.WriteLine($"{this} v={v}| Reply: s={s}, r={item.R}");
            var rr = _node.Commit(this, s, item.R);
            for (var i = 0; i < rr.Length; i++)
            {
                var r = rr[i];
                if (_timerByR.ContainsKey(r) == true)
                {
                    _timerByR[r].Dispose();
                    _timerByR.Remove(r);
                    Console.WriteLine($"{this} v={_v}| Timer disposed: r={r}");
                }
            }
        }
    }

    private Timer? _viewTimer;

    public void OnViewChange(int v1, (int s, int r)[] Pb, int b)
    {
        _dispatcher.VerifyAccess();
        if (v1 != _v + 1)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} v={_v}| ViewChange: v1={v1}, b={b}");
        _viewChangeMessages.AddRange(Pb);
        _vc++;

        var minimum = 2 * _f + 1;
        if (_vc != minimum)
            return;

        var isPrimary = v1 % _endPoints.Length == _ni;
        if (isPrimary == true)
        {
            var maximumS = _prePrepareMessages.Count != 0 ? _prePrepareMessages.Max(item => item.S) : 0;
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
            if (_node.IsByzantine != true)
            {
                Console.WriteLine($"{this} v={_v}| Broadcast NewView: v1={v1}, p={p}");
                _broadcaster.SendAll(item => item.NewView(v1, V, O, p));
            }
            else
            {
                Console.WriteLine($"{this} v={_v}| primary is byzantine");
            }
        }
    }

    public void OnNewView(int v, (int s, int r)[] V, (int s, int r)[] O, int p)
    {
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (p != _p)
            throw new InvalidOperationException();
        if (_isNew == true)
            throw new InvalidOperationException();

        Console.WriteLine($"{this} v={_v}| NewView: v1={v}, p={p}");
        _viewTimer?.Dispose();
        _viewTimer = null;
        _certificateMessages.AddRange(O);
        _prePrepareMessages.AddRange(V);
        _isNew = true;
        _s = _prePrepareMessages.Count > 0 ? _prePrepareMessages.Max(item => item.S) : 0;
    }

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        _dispatcher.Dispose();
        _isDisposed = true;
    }

    private bool IsNotMe(EndPoint endPoint) => endPoint != _node.EndPoint;
}
