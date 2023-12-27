using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using JSSoft.Library.Commands.Extensions;
using JSSoft.Library.Terminals;

namespace DistributedLedgers.ConsoleHost;

sealed partial class Application : IAsyncDisposable, IServiceProvider
{
    private readonly CompositionContainer _container;
    private readonly ApplicationServiceCollection _applicationServices;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isDisposed;
    private SystemTerminal? _terminal;
    private readonly ApplicationOptions _options = new();

    public Application(ApplicationOptions options)
    {
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        SynchronizationContext.SetSynchronizationContext(new());
        ConsoleTextWriter.SynchronizationContext = SynchronizationContext.Current!;
        _options = options;
        _container = new(new AssemblyCatalog(typeof(Application).Assembly));
        _container.ComposeExportedValue(this);
        _container.ComposeExportedValue<IServiceProvider>(this);
        _container.ComposeExportedValue(_options);
        _applicationServices = new(_container.GetExportedValues<IApplicationService>());
    }

    public void Cancel()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }

    public async Task StartAsync()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");
        if (_terminal != null)
            throw new InvalidOperationException("Application has already been started.");

        await _applicationServices.InitializeAsync(this, cancellationToken: default);
        await PrepareCommandContext();
        _cancellationTokenSource = new();
        _terminal = _container.GetExportedValue<SystemTerminal>()!;
        await _terminal!.StartAsync(_cancellationTokenSource.Token);

        async Task PrepareCommandContext()
        {
            var sw = new StringWriter();
            var commandContext = GetService<CommandContext>()!;
            commandContext.Out = sw;
            sw.WriteLine(TerminalStringBuilder.GetString("============================================================", TerminalColorType.BrightGreen));
            await commandContext.ExecuteAsync(new string[] { "--help" }, cancellationToken: default);
            sw.WriteLine();
            await commandContext.ExecuteAsync(Array.Empty<string>(), cancellationToken: default);
            sw.WriteLine(TerminalStringBuilder.GetString("============================================================", TerminalColorType.BrightGreen));
            commandContext.Out = Console.Out;
            Console.Write(sw.ToString());
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
        _container.Dispose();
        await _applicationServices.DisposeAsync();
        _terminal = null;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    public T? GetService<T>()
    {
        return _container.GetExportedValue<T>();
    }

    public object? GetService(Type serviceType)
    {
        return _container.GetExportedValue<object?>(AttributedModelServices.GetContractName(serviceType));
    }
}
