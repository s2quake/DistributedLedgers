using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;

namespace DistributedLedgers.ConsoleHost;

static class PortUtility
{
    private static readonly List<int> reservedPortList = new();
    private static readonly List<int> usedPortList = new();

    public static int GetPort()
    {
        if (reservedPortList.Count == 0)
        {
            var v = GetRandomPort();
            while (reservedPortList.Contains(v) == true || usedPortList.Contains(v) == true)
            {
                v = GetRandomPort();
            }
            reservedPortList.Add(v);
        }
        var port = reservedPortList[0];
        reservedPortList.Remove(port);
        usedPortList.Add(port);
        return port;
    }

    public static int[] GetPorts(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var ports = new int[count];
        for (var i = 0; i < ports.Length; i++)
        {
            ports[i] = GetPort();
        }
        return ports;
    }

    public static void ReleasePort(int port)
    {
        reservedPortList.Add(port);
        usedPortList.Remove(port);
    }

    public static void ReleasePorts(int[] ports)
    {
        for (var i = 0; i < ports.Length; i++)
        {
            ReleasePort(ports[i]);
        }
    }

    private static int GetRandomPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
