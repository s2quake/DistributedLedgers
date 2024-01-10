namespace DistributedLedgers.ConsoleHost.Common;

static class RandomUtility
{
    public static ulong NextULong() => NextULong(ulong.MinValue, ulong.MaxValue);

    public static ulong NextULong(ulong max) => NextULong(ulong.MinValue, max);

    public static ulong NextULong(ulong min, ulong max)
    {
        var m1 = (long)(min - long.MaxValue - 1);
        var m2 = (long)(max - long.MaxValue - 1);
        var m = Random.Shared.NextInt64(m1, m2);
        var v = (ulong)(m + long.MaxValue + 1);
        return v;
    }

    public static byte NextByte(byte min, byte max)
    {
        var m1 = (sbyte)(min - sbyte.MaxValue - 1);
        var m2 = (sbyte)(max - sbyte.MaxValue - 1);
        var m = (sbyte)Random.Shared.Next(m1, m2);
        var v = (byte)(m + sbyte.MaxValue + 1);
        return v;
    }

    public static ulong NextULong(ulong min, ulong max, Predicate<ulong> predicate)
    {
        var tryCount = 10;
        for (var i = 0; i < tryCount; i++)
        {
            var v = NextULong(min, max);
            if (predicate(v) == true)
            {
                return v;
            }
        }
        throw new NotImplementedException("Cannot found value that matches condition.");
    }
}
