namespace DistributedLedgers.ConsoleHost;

interface IBroadcastService
{
    void Broadcast(object sender, int type, params object[] args);
}
