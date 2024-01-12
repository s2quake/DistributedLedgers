using System.Net;
using JSSoft.Communication;

namespace DistributedLedgers.ConsoleHost.Common;

interface INode
{
    EndPoint EndPoint { get; }

    IReadOnlyList<INode> Nodes { get; }
}