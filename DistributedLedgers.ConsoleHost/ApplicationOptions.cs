using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost;

[Export]
sealed class ApplicationOptions
{
    public static ApplicationOptions Parse(string[] args)
    {
        var options = new ApplicationOptions();
        var parserSettings = new CommandSettings()
        {
            AllowEmpty = true,
        };
        var parser = new CommandParser(options, parserSettings);
        parser.Parse(args);
        return options;
    }
}
