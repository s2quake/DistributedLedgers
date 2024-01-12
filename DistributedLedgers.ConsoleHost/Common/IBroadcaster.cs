using System.Net;

namespace DistributedLedgers.ConsoleHost.Common;

interface IBroadcaster<T, TService>
    where T : NodeBase<T, TService>
    where TService : class
{
    void SendAll(Action<TService> action) => SendAll(action, item => true);

    void SendAll(Action<TService> action, Predicate<EndPoint> predicate);

    void Send(EndPoint endPoint, Action<TService> action);
}
