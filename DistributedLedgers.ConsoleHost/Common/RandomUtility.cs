namespace DistributedLedgers.ConsoleHost.Common;

static class RandomUtility
{
    public static ulong NextULong(ulong min, ulong max)
    {
        var hight = Random.Shared.Next((int)(min >> 32), (int)(max >> 32));
        var minLow = Math.Min((int)min, (int)max);
        var maxLow = Math.Max((int)min, (int)max);
        var low = (uint)Random.Shared.Next(minLow, maxLow);
        ulong result = (ulong)hight;
        result <<= 32;
        result |= (ulong)low;
        return result;
    }
}
