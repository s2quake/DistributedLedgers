using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace DistributedLedgers.ConsoleHost.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
sealed class ConfigCommand(ApplicationConfigurations configurations) : CommandBase
{
    private readonly ApplicationConfigurations _configurations = configurations;

    [CommandPropertyRequired(DefaultValue = "")]
    public string Key { get; set; } = string.Empty;

    [CommandPropertyRequired(DefaultValue = "")]
    public string Value { get; set; } = string.Empty;

    protected override void OnExecute()
    {
        if (Key == string.Empty && Value == string.Empty)
        {
            foreach (var item in _configurations.Descriptors)
            {
                Out.Write(item);
            }
        }
        else if (Value == string.Empty)
        {
            Out.WriteLine($"{_configurations.GetValue(Key)}");
        }
        else
        {
            _configurations.SetValue(Key, Value);
        }
    }
}
