namespace DistributedLedgers.ConsoleHost.Common;

static class ByzantineUtility
{
    public static int GetByzantineCount(in int n, Func<int, int, bool> func)
    {
        var f = n;
        while (f > 0)
        {
            if (func(n, f) == true)
                break;
            f--;
        }
        return f;
    }
}
