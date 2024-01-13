using System.ComponentModel;
using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter5;

[Export(typeof(ICommand))]
[Category("Chapter 5")]
sealed class Algorithm_5_2Command : CommandBase
{
    public Algorithm_5_2Command()
        : base("algo-5-2")
    {
    }

    [CommandPropertyRequired]
    public ulong PrimeNumber { get; set; }

    [CommandPropertyRequired]
    public ulong PrimeRoot { get; set; }

    protected override void OnExecute()
    {
        var p = PrimeNumber;
        var g = PrimeRoot;
        var a = "👩";
        var b = "🧑";
        Out.WriteLine("============ Diffie-Hellman key exchange ============");

        var ka1 = RandomUtility.NextULong(1, p - 2 + 1);
        var ka2 = Math.Pow(g, ka1) % p;
        Out.WriteLine($"1. {a} selects {ka1} and sends {ka2} to {b}");

        var kb1 = RandomUtility.NextULong(1, p - 2 + 1);
        var kb2 = Math.Pow(g, kb1) % p;
        Out.WriteLine($"2. {b} selects {kb1} and sends {kb2} to {a}");

        var ka3 = Math.Pow(kb2, ka1) % p;
        Out.WriteLine($"3. {a} computes secret key: {ka3}");

        var kb3 = Math.Pow(ka2, kb1) % p;
        Out.WriteLine($"4. {b} computes secret key: {kb3}");

        if (ka3 != kb3)
            throw new InvalidOperationException($"{ka3} is not equals to {kb3}");

        var secretKey = ka1;
        Out.WriteLine($"5. both {a} and {b} have common secret key: {secretKey}");
    }
}
