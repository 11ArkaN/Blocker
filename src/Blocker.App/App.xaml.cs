using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Blocker.App.Constants;
using Blocker.App.Contracts;
using Blocker.App.Services;
using Blocker.App.ViewModels;

namespace Blocker.App;

public partial class App : System.Windows.Application
{
    private ILogService? _logger;
    private IStateStore? _stateStore;
    private IBlockOrchestrator? _orchestrator;
    private IProcessWatchdog? _watchdog;
    private IStartupService? _startupService;
    private MainWindowViewModel? _viewModel;
    private MainWindow? _mainWindow;
    private TrayService? _trayService;
    private bool _isShuttingDown;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HookGlobalExceptionHandlers();

        try
        {
            await InitializeAsync(e.Args);
        }
        catch (Exception ex)
        {
            _logger?.Error("Fatal startup exception.", ex);
            System.Windows.MessageBox.Show(
                $"Application startup failed:{Environment.NewLine}{ex.Message}",
                "Blocker",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;

        try
        {
            ShutdownServicesAsync().GetAwaiter().GetResult();
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private async Task InitializeAsync(string[] args)
    {
        _logger = new FileLogService();
        _stateStore = new JsonStateStore(_logger);

        var appExecutablePath = GetExecutablePath();
        var processRunner = new ProcessRunner();
        var processManager = new ProcessManager(_logger);
        var targetCatalog = new TargetCatalog();
        _watchdog = new ProcessWatchdogService(processManager, _logger);
        var focusLockService = new FocusLockService();
        var guardianService = new GuardianService(_logger, appExecutablePath);

        var providers = new IBlockingProvider[]
        {
            new FirewallProgramBlockingProvider(processRunner, _logger),
            new HostsBlockingProvider(_logger)
        };

        _orchestrator = new BlockOrchestrator(
            providers,
            _watchdog,
            processManager,
            targetCatalog,
            _stateStore,
            focusLockService,
            guardianService,
            _logger);

        _startupService = new StartupService(_logger);
        _startupService.EnableAutoStart(appExecutablePath, startInTray: true);

        var startInTray = args.Any(arg =>
            arg.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
        var resumeGuarded = args.Any(arg =>
            arg.Equals("--resume-guarded", StringComparison.OrdinalIgnoreCase));

        await RecoverFromUncleanShutdownIfNeededAsync(resumeGuarded);
        await (_orchestrator as BlockOrchestrator)!.InitializeFromPersistedStateAsync(
            resumeActiveRuntime: resumeGuarded);
        await _stateStore.MarkSessionStartedAsync();

        _viewModel = new MainWindowViewModel(_orchestrator, _logger);
        await _viewModel.InitializeAsync();
        _viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(MainWindowViewModel.IsBlockActive))
            {
                SyncTrayState();
            }
        };

        _mainWindow = new MainWindow(_viewModel);
        _trayService = new TrayService();
        WireTrayEvents();
        _trayService.SetBlockState(_viewModel.IsBlockActive);

        if (startInTray)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
        }
    }

    private async Task RecoverFromUncleanShutdownIfNeededAsync(bool resumeGuarded)
    {
        if (_stateStore is null || _orchestrator is null || _logger is null)
        {
            return;
        }

        var state = await _stateStore.LoadAsync();
        if (!state.IsBlockActive || state.LastShutdownClean)
        {
            return;
        }

        if (resumeGuarded)
        {
            _logger.Info("Unclean shutdown detected but resume-guarded mode is active.");
            return;
        }

        _logger.Warn("Detected unclean shutdown with active block. Restoring access.");
        var result = await _orchestrator.DisableAsync(BlockerConstants.UnlockPhrase);
        if (!result.Success)
        {
            _logger.Error("Recovery disable operation completed with errors.");
        }
    }

    private void WireTrayEvents()
    {
        if (_trayService is null)
        {
            return;
        }

        _trayService.OpenRequested += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        _trayService.EnableRequested += (_, _) => _ = InvokeOnUiAsync(async () =>
        {
            if (_viewModel is not null)
            {
                await _viewModel.EnableBlockAsync();
                SyncTrayState();
            }
        });
        _trayService.DisableRequested += (_, _) => _ = InvokeOnUiAsync(async () =>
        {
            if (_viewModel is not null)
            {
                await _viewModel.DisableBlockAsync();
                SyncTrayState();
            }
        });
        _trayService.ExitRequested += (_, _) => Dispatcher.Invoke(RequestShutdown);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void RequestShutdown()
    {
        if (_viewModel?.IsBlockActive == true)
        {
            System.Windows.MessageBox.Show(
                "Disable blocking before exiting the app.",
                "Blocker",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.Close();
        }

        Shutdown();
    }

    private async Task ShutdownServicesAsync()
    {
        if (_orchestrator is not null)
        {
            var current = await _orchestrator.GetStateAsync();
            if (current.IsActive)
            {
                await _orchestrator.DisableAsync(BlockerConstants.UnlockPhrase);
            }

            current = await _orchestrator.GetStateAsync();
            if (_stateStore is not null)
            {
                var isClean = !current.IsActive;
                await _stateStore.MarkSessionEndedAsync(
                    current.IsActive,
                    current.ActivatedAt,
                    current.FocusLockUntil,
                    current.IsActive,
                    isClean);
            }
        }

        _trayService?.Dispose();
        if (_watchdog is IDisposable disposableWatchdog)
        {
            disposableWatchdog.Dispose();
        }
    }

    private Task InvokeOnUiAsync(Func<Task> action)
    {
        return Dispatcher.InvokeAsync(action, DispatcherPriority.Normal).Task.Unwrap();
    }

    private void SyncTrayState()
    {
        if (_trayService is null || _viewModel is null)
        {
            return;
        }

        _trayService.SetBlockState(_viewModel.IsBlockActive);
    }

    private static string GetExecutablePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(assemblyLocation))
        {
            var exeCandidate = Path.ChangeExtension(assemblyLocation, ".exe");
            if (File.Exists(exeCandidate))
            {
                return exeCandidate;
            }

            return assemblyLocation;
        }

        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine executable path.");
    }

    private void HookGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += HandleDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += HandleAppDomainUnhandledException;
    }

    private void HandleDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error("Unhandled UI exception.", e.Exception);
        if (_isShuttingDown)
        {
            return;
        }

        System.Windows.MessageBox.Show(
            $"Unexpected error:{Environment.NewLine}{e.Exception.Message}",
            "Blocker",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);

        e.Handled = false;
    }

    private void HandleAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger?.Error("Unhandled background exception.", exception);
        }
        else
        {
            _logger?.Error("Unhandled background exception (non-Exception object).");
        }
    }
}
