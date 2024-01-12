
using System.ComponentModel.Composition;
using System.Xml.Serialization;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter5;

[Export(typeof(ICommand))]
sealed class Algorithm_5_4Command : CommandAsyncBase
{
    public Algorithm_5_4Command()
        : base("alg-5-4")
    {
    }

    [CommandPropertyRequired(DefaultValue = ulong.MaxValue)]
    public ulong PrimeNumber { get; set; }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        await Task.CompletedTask;
        var p = PrimeNumber == ulong.MaxValue ? GetOddNumber() : PrimeNumber;
        if (p % 2 == 0)
            throw new InvalidOperationException($"'{p}' is not odd number.");

        var z = new ulong[]
        {
            11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97
        };
        p = z[Random.Shared.Next(z.Length)];

        // p = 17;
        // p = 31;

        var js = GetValues(p).ToArray();
        if (js.Length == 0)
            throw new InvalidOperationException($"{p} is not prime number -- ");
        var j = js[Random.Shared.Next(js.Length)];
        var r = (ulong)Math.Log((p - 1) / 2, 2);
        var x = RandomUtility.NextULong(1, p);
        var x0 = Math.Pow(x, j) % p;
        if (x0 == 1 || x0 == (p - 1))
        {
            Out.WriteLine($"{p} is prime number: step 1");
            return;
        }

        for (var i = 1ul; i <= r - 1; i++)
        {
            var xi = Math.Pow(x0, 2) % p;
            if (xi == p - 1)
            {
                Out.WriteLine($"{p} is prime number: step 2");
                return;
            }
            x0 = xi;
        }

        throw new InvalidOperationException($"{p} is not prime number");
    }

    private static IEnumerable<ulong> GetValues(ulong primeNumber)
    {
        var p = primeNumber;
        for (ulong i = 1; i < p - 1; i += 2)
        {
            var j = i;
            var r = Math.Log((p - 1) / j, 2);
            if ((r % 1 == 0) && (p - 1) == Math.Pow(2, r) * j)
            {
                yield return j;
            }
        }
    }

    private static ulong GetOddNumber()
    {
        var i = ulong.MaxValue;
        do
        {
            i = RandomUtility.NextULong();
        } while (i % 2 == 0);
        return i;
    }
}
