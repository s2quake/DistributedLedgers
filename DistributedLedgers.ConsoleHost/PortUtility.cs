using System.Net;
using System.Net.Sockets;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost;

static class PortUtility
{
    public static int GetPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public static int[] GetPorts(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var listeners = new TcpListener[count];
        var ports = new int[count];
        for (var i = 0; i < ports.Length; i++)
        {
            listeners[i] = new TcpListener(IPAddress.Loopback, 0);
            listeners[i].Start();
            ports[i] = ((IPEndPoint)listeners[i].LocalEndpoint).Port;
        }
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].Stop();
        }
        return ports;
    }

    public static EndPoint GetEndPoint()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new DnsEndPoint(ServiceContextBase.DefaultHost, ((IPEndPoint)listener.LocalEndpoint).Port);
    }

    public static EndPoint[] GetEndPoints(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var listeners = new TcpListener[count];
        var endPoints = new EndPoint[count];
        for (var i = 0; i < endPoints.Length; i++)
        {
            listeners[i] = new(IPAddress.Loopback, 0);
            listeners[i].Start();
            endPoints[i] = new DnsEndPoint(ServiceContextBase.DefaultHost, ((IPEndPoint)listeners[i].LocalEndpoint).Port);
        }
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].Stop();
        }
        return endPoints;
    }
}
