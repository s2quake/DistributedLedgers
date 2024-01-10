
using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter5;

[Export(typeof(ICommand))]
sealed class Algorithm_5_4Command : CommandBase
{
    public Algorithm_5_4Command()
        : base("alg-5-4")
    {
    }

    [CommandPropertyRequired(DefaultValue = ulong.MaxValue)]
    public ulong PrimeNumber
    {
        get; set;
    }

    protected override void OnExecute()
    {
        var p = PrimeNumber == ulong.MaxValue ? GetOddNumber() : PrimeNumber;
        if (p % 2 == 0)
            throw new InvalidOperationException($"'{p}' is not odd number.");

        var j = GetOddNumber();

        p = 13;
        j = 9;
        var r = Math.Log((p - 1) / j) / Math.Log(2);
        var x = GetX(p);


        Out.WriteLine($"{p}, {x}");
    }

    private static ulong GetOddNumber()
    {
        var i = ulong.MaxValue;
        while ((i = (ulong)Random.Shared.NextInt64()) % 2 == 0)
        {
        }
        return i;
    }

    private static ulong GetX(ulong p)
    {
        var min = 1L - long.MaxValue;
        var max = (long)(p - long.MaxValue);
        return (uint)(Random.Shared.NextInt64(min, max) + long.MaxValue);
    }
}
