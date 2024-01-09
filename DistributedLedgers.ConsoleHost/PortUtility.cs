using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;

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
}
