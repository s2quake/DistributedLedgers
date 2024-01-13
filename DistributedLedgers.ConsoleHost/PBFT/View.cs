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
    private readonly EndPoint _endPoint;
    private readonly RequestMessageCollection _requestMessages = [];
    private readonly PrepareMessageCollection _prePrepareMessages = [];
    private readonly PrepareMessageCollection _prepareMessages = [];
    private readonly PrepareMessageCollection _cerificateMessages = [];
    private readonly CommitMessageCollection _commitMessages = [];
    private readonly Dispatcher _dispatcher;
    private bool _isDisposed;
    private int _s;

    public View(int v, EndPoint[] endPoints, int f, Node node, IBroadcaster broadcaster)
    {
        _v = v;
        _endPoints = endPoints;
        _f = f;
        _ni = node.Index;
        _p = _v % _endPoints.Length;
        _node = node;
        _broadcaster = broadcaster;
        _endPoint = node.EndPoint;
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
        Console.WriteLine($"{this} Request: r={r}, c={c}");
        var isPrimary = IsPrimary;
        var ni = _ni;

        var s = Interlocked.Increment(ref _s);
        _requestMessages.Add(r: r, c: c, s: s);
        if (isPrimary == true)
        {
            _broadcaster.SendAll(service => service.PrePrepare(_v, s, r, p: ni), predicate: IsNotMe);
        }
        else
        {
            // SendRequestToPrimary(_v, r, c, ni);
        }
    }

    private bool IsPrimaryNode(int ni)
    {
        return _v % _endPoints.Length == ni;
    }

    public void PrePrepare(int v, int s, int r, int p)
    {
        Console.WriteLine($"{this} PrePrepare: v={v}, s={s}, r={r}, p={p}");
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (p != _p)
            throw new InvalidOperationException();

        var b = _ni;

        if (IsPrimaryNode(ni: p) == true && _prePrepareMessages.Contains(s: s, r: r) == false)
        {
            _prePrepareMessages.Add(s: s, r: r);
            _prepareMessages.Add(s: s, r: r);
            _broadcaster.SendAll(service => service.Prepare(v, s, r, b), predicate: IsNotMe);
        }
    }

    public void Prepare(int v, int s, int r, int b)
    {
        Console.WriteLine($"{this} Prepare: v={v}, s={s}, r={r}, n={b}");
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (b == _p)
            throw new InvalidOperationException();

        var minimum = 2 * _f;
        var ni = _ni;
        _prepareMessages.Add(s: s, r: r);
        if (_prepareMessages.CanCommit(s: s, r: r, minimum) == true)
        {
            _cerificateMessages.Add(s: s, r: r);
            _commitMessages.Add(s: s);
            _broadcaster.SendAll(service => service.Commit(v, s, _node.Index), predicate: IsNotMe);
        }
    }

    public void Commit(int v, int s, int ni)
    {
        Console.WriteLine($"{this} Commit: v={v}, s={s}, n={ni}");
        _dispatcher.VerifyAccess();
        if (v != _v)
            throw new InvalidOperationException();
        if (ni == _ni)
            throw new InvalidOperationException();

        var minimum = 2 * _f + 1;
        if (_commitMessages.CanReply(s: s, minimum) == true)
        {
            var items = _requestMessages.Remove(s: s);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                _node.Reply(item.R, item.C);
                // Console.WriteLine($"{this} Reply: v={v}, s={item.S}, r={item.R}, c={item.C}");
            }
        }
    }

    private int _vc = 0;
    public void ViewChange((int s, int r)[] Pb1, (int s, int r)[] Pb2)
    {
        _dispatcher.VerifyAccess();
        _prePrepareMessages.AddRange(Pb1);
        _prepareMessages.AddRange(Pb2);
        _vc++;

        var minimum = 2 * _f + 1;
        if (_vc == minimum)
        {
            var isPrimary = IsPrimary;
            if (isPrimary == true)
            {
                // var maximumS = _prePrepareMessages.GetMaximumS();

                int qwrwqer = 0;
            }
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
        // var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        // try
        // {
        //     await _node.SendRequestAsync(_primaryEndPoint, _v, r, c, ni, cancellationTokenSource.Token);
        // }
        // catch
        {
            await Task.Delay(Random.Shared.Next(100, 200));
            await _dispatcher.InvokeAsync(() =>
            {
                var v = _v;
                var Pb1 = _prePrepareMessages.Collect();
                var Pb2 = _prepareMessages.Collect();
                var b = _ni;
                _broadcaster.SendAll(service => service.ViewChange(v + 1, Pb1, Pb2, b));
            });
        }
    }

    private bool IsNotMe(EndPoint endPoint) => endPoint != _endPoint;
}
