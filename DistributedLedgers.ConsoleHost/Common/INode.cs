using System.Net;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

interface INode
{
    DnsEndPoint EndPoint { get; }

    IReadOnlyList<INode> Nodes { get; }
}