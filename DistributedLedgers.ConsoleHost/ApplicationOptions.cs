using System.ComponentModel.Composition;
using JSSoft.Library.Commands;

namespace DistributedLedgers.ConsoleHost;

[Export]
sealed class ApplicationOptions
{
    public static ApplicationOptions Parse(string[] args)
    {
        var options = new ApplicationOptions();
        var parser = new InternalCommandParser(options);
        parser.Parse(args);
        return options;
    }

    #region InternalCommandParser

    sealed class InternalCommandParser : CommandParser
    {
        public InternalCommandParser(object instance)
            : base(instance)
        {
        }

        protected override void OnValidate(string[] args)
        {
            if (args.Length != 0)
            {
                base.OnValidate(args);
            }
        }
    }

    #endregion
}
