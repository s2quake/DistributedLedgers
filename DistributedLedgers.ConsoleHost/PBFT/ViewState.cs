namespace DistributedLedgers.ConsoleHost.PBFT;

enum ViewState
{
    Request,

    PrePrepare,

    Prepare,

    Commit,

    Reply,
}
