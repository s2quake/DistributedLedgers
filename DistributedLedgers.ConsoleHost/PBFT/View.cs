using System.Net;
using JSSoft.Communication.Threading;
using JSSoft.Terminals;
using IBroadcaster = DistributedLedgers.ConsoleHost.Common.IBroadcaster<DistributedLedgers.ConsoleHost.PBFT.Node, DistributedLedgers.ConsoleHost.PBFT.INodeService>;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class View : IDisposable
{
    private readonly int _v;
    private readonly EndPoint[] _endPoints;
    private readonly int _f;
    private readonly int _ni;
    private readonly Node _node;
    private readonly IBroadcaster _broadcaster;
    private readonly EndPoint _endPoint;
    private readonly RequestMessageCollection _requestMessages = [];
    private readonly PrePrepareMessageCollection _prePrepareMessages = [];
    private readonly PrepareMessageCollection _prepareMessages = [];
    private readonly CommitMessageCollection _commitMessages = [];
    private readonly EndPoint _primaryEndPoint;
    private readonly Dispatcher _dispatcher;
    private bool _isDisposed;
    private int _s;

    public View(int v, EndPoint[] endPoints, int f, Node node, IBroadcaster broadcaster)
    {
        _v = v;
        _endPoints = endPoints;
        _f = f;
        _ni = node.Index;
        _node = node;
        _broadcaster = broadcaster;
        _endPoint = node.EndPoint;
        _primaryEndPoint = endPoints[v % endPoints.Length];
        _dispatcher = new(this);
    }

    public int Index => _v;

    public Dispatcher Dispatcher => _dispatcher;

    public bool IsPrimary => _v % _endPoints.Length == _ni;

    public bool IsBackup => _v % _endPoints.Length != _ni;

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
        // Console.WriteLine($"{this} PrePrepare: v={v}, s={s}, r={r}, p={p}");
        _dispatcher.VerifyAccess();
        if (_ni == _v)
            throw new InvalidOperationException();

        var b = _ni;
        if (IsPrimaryNode(ni: p) == true && _prePrepareMessages.Add(v: v, s: s, r: r, p: p) == true)
        {
            _prepareMessages.Add(v: v, s: s, r: r);
            _broadcaster.SendAll(service => service.Prepare(v, s, r, b), predicate: IsNotMe);
        }
    }

    public void Prepare(int v, int s, int r, int b)
    {
        // Console.WriteLine($"{this} Prepare: v={v}, s={s}, r={r}, n={ni}");
        _dispatcher.VerifyAccess();

        var minimum = 2 * _f;
        var ni = _ni;
        _prepareMessages.Add(v: v, s: s, r: r);
        if (_prepareMessages.CanCommit(v: v, s: s, r: r, minimum) == true)
        {
            _commitMessages.Add(v: v, s: s, ni);
            _broadcaster.SendAll(service => service.Commit(v, s, _node.Index), predicate: IsNotMe);
        }
    }

    public void Commit(int v, int s, int ni)
    {
        // Console.WriteLine($"{this} Commit: v={v}, s={s}, n={ni}");
        _dispatcher.VerifyAccess();

        var minimum = 2 * _f + 1;
        if (_commitMessages.CanReply(v: v, s: s, ni: ni, minimum) == true)
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

    public void ViewChange((int s, int r)[] Pb)
    {
        _dispatcher.VerifyAccess();
        _prepareMessages.AddRange(_v, Pb);

        var isPrimary = _v % _endPoints.Length == _ni;
        if (isPrimary == true)
        {
            int qwrwqer = 0;
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
                var Pb = _prepareMessages.Collect();
                var b = _ni;
                _broadcaster.SendAll(service => service.ViewChange(v + 1, Pb, b));
            });
        }
    }

    private bool IsNotMe(EndPoint endPoint) => endPoint != _endPoint;
}
