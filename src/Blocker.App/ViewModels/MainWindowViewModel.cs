using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Blocker.App.Constants;
using Blocker.App.Contracts;
using Blocker.App.Models;
using Wpf.Ui.Controls;

namespace Blocker.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IBlockOrchestrator _orchestrator;
    private readonly ILogService _logger;
    private readonly ILocalizationService _localization;
    private readonly RelayCommand _toggleCommand;
    private readonly RelayCommand _refreshCommand;
    private readonly DispatcherTimer _uptimeTimer;

    private bool _isBlockActive;
    private bool _isBusy;
    private bool _isFocusPhraseRequired;
    private bool _isAdmin;
    private bool _isGuardianHealthy;
    private DateTimeOffset? _activatedAtUtc;
    private DateTimeOffset? _focusLockUntilUtc;
    private string? _unlockPhrase;
    private string _activeSinceText;
    private string _focusLockText;
    private string _selectedLanguageCode;
    private bool _isApplyingLanguageSelection;
    private BlockState? _lastState;

    public MainWindowViewModel(IBlockOrchestrator orchestrator, ILogService logger, ILocalizationService localization)
    {
        _orchestrator = orchestrator;
        _logger = logger;
        _localization = localization;

        _selectedLanguageCode = localization.CurrentLanguageCode;
        _activeSinceText = L("Main.ActiveSinceNone");
        _focusLockText = L("Main.FocusLockNone");

        _toggleCommand = new RelayCommand(_ => _ = ToggleBlockAsync(), _ => !IsBusy);
        _refreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsBusy);

        _localization.LanguageChanged += HandleLanguageChanged;

        _uptimeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uptimeTimer.Tick += (_, _) =>
        {
            UpdateActiveSinceText();
            UpdateFocusLockText();
            OnPropertyChanged(nameof(FocusProgressPercent));
        };
        _uptimeTimer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> ActivityLog { get; } = new();

    public ICommand ToggleBlockCommand => _toggleCommand;

    public ICommand RefreshCommand => _refreshCommand;

    public ILocalizationService Localization => _localization;

    public Func<string?, Task<string?>>? RequestUnlockPhraseAsync { get; set; }
    public Func<Task<string?>>? RequestSetupUnlockPhraseAsync { get; set; }

    public string SelectedLanguageCode
    {
        get => _selectedLanguageCode;
        set
        {
            if (string.Equals(_selectedLanguageCode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedLanguageCode = value;
            OnPropertyChanged();

            if (!_isApplyingLanguageSelection)
            {
                _ = ApplyLanguageSelectionAsync(value);
            }
        }
    }

    public bool IsBlockActive
    {
        get => _isBlockActive;
        private set
        {
            if (_isBlockActive == value)
            {
                return;
            }

            _isBlockActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(ToggleButtonAppearance));
            OnPropertyChanged(nameof(StatusTitle));
            OnPropertyChanged(nameof(StatusBadgeText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusSeverity));
            OnPropertyChanged(nameof(ModeBadgeAppearance));
            OnPropertyChanged(nameof(GuardianStatus));
            OnPropertyChanged(nameof(GuardianBadgeAppearance));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanInteract));
            _toggleCommand.RaiseCanExecuteChanged();
            _refreshCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanInteract => !IsBusy;

    public bool IsFocusPhraseRequired
    {
        get => _isFocusPhraseRequired;
        private set
        {
            if (_isFocusPhraseRequired == value)
            {
                return;
            }

            _isFocusPhraseRequired = value;
            OnPropertyChanged();
        }
    }

    public string StatusText => IsBlockActive ? L("Main.StatusActive") : L("Main.StatusInactive");

    public string AdminStatus => _isAdmin ? L("Common.Yes") : L("Common.No");

    public string GuardianStatus
    {
        get
        {
            if (!IsBlockActive)
            {
                return L("Main.NotApplicable");
            }

            return _isGuardianHealthy ? L("Main.GuardianActive") : L("Main.GuardianUnavailable");
        }
    }

    public string ActiveSinceText
    {
        get => _activeSinceText;
        private set
        {
            if (_activeSinceText == value)
            {
                return;
            }

            _activeSinceText = value;
            OnPropertyChanged();
        }
    }

    public string FocusLockText
    {
        get => _focusLockText;
        private set
        {
            if (_focusLockText == value)
            {
                return;
            }

            _focusLockText = value;
            OnPropertyChanged();
        }
    }

    public string UnlockPhraseDisplay
    {
        get
        {
            var label = L("Main.PhraseLabel");
            return string.IsNullOrWhiteSpace(_unlockPhrase) ? $"{label} -" : $"{label} {_unlockPhrase}";
        }
    }

    public string ToggleButtonText => IsBlockActive ? L("Main.ToggleOff") : L("Main.ToggleOn");

    public string StatusTitle => IsBlockActive ? L("Main.StatusTitleActive") : L("Main.StatusTitleInactive");

    public string StatusBadgeText => IsBlockActive ? L("Main.StatusBadgeOn") : L("Main.StatusBadgeOff");

    public ControlAppearance ToggleButtonAppearance => IsBlockActive ? ControlAppearance.Danger : ControlAppearance.Success;

    public InfoBarSeverity StatusSeverity => IsBlockActive ? InfoBarSeverity.Success : InfoBarSeverity.Informational;

    public ControlAppearance ModeBadgeAppearance => IsBlockActive ? ControlAppearance.Success : ControlAppearance.Secondary;

    public ControlAppearance AdminBadgeAppearance => _isAdmin ? ControlAppearance.Success : ControlAppearance.Danger;

    public ControlAppearance GuardianBadgeAppearance
    {
        get
        {
            if (!IsBlockActive)
            {
                return ControlAppearance.Secondary;
            }

            return _isGuardianHealthy ? ControlAppearance.Success : ControlAppearance.Danger;
        }
    }

    public double FocusProgressPercent
    {
        get
        {
            if (!IsBlockActive || _focusLockUntilUtc is null)
            {
                return 0;
            }

            var duration = TimeSpan.FromMinutes(BlockerConstants.FocusLockMinutes);
            var remaining = _focusLockUntilUtc.Value - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return 100;
            }

            var elapsed = duration - remaining;
            var ratio = elapsed.TotalSeconds / duration.TotalSeconds;
            if (ratio < 0)
            {
                ratio = 0;
            }
            else if (ratio > 1)
            {
                ratio = 1;
            }

            return ratio * 100;
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
        AddLog(L("Main.LogAppStarted"));
    }

    public async Task EnableBlockAsync()
    {
        if (IsBlockActive || IsBusy)
        {
            return;
        }

        await SetBlockStateAsync(enable: true);
    }

    public async Task DisableBlockAsync()
    {
        if (!IsBlockActive || IsBusy)
        {
            return;
        }

        await SetBlockStateAsync(enable: false);
    }

    private async Task ToggleBlockAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await SetBlockStateAsync(enable: !IsBlockActive);
    }

    private async Task SetBlockStateAsync(bool enable)
    {
        IsBusy = true;
        AddLog(enable ? L("Main.LogEnabling") : L("Main.LogDisabling"));

        try
        {
            BlockResult result;
            if (enable)
            {
                if (RequestSetupUnlockPhraseAsync is null)
                {
                    AddLog(L("Main.LogNoSetupDialog"));
                    return;
                }

                var newPhrase = await RequestSetupUnlockPhraseAsync();
                if (newPhrase is null)
                {
                    AddLog(L("Main.LogEnableCancelled"));
                    return;
                }

                result = await _orchestrator.EnableAsync(newPhrase);
            }
            else
            {
                string? phrase = null;
                if (IsFocusPhraseRequired && RequestUnlockPhraseAsync is not null)
                {
                    phrase = await RequestUnlockPhraseAsync(_unlockPhrase);
                    if (phrase is null)
                    {
                        AddLog(L("Main.LogDisableCancelled"));
                        return;
                    }
                }

                result = await _orchestrator.DisableAsync(phrase);
            }

            foreach (var message in result.Messages)
            {
                AddLog(message);
            }

            foreach (var error in result.Errors)
            {
                AddLog($"{L("Main.LogErrorPrefix")} {error}");
            }

            if (!result.Success)
            {
                AddLog(L("Main.LogOperationWithErrors"));
            }
        }
        catch (Exception ex)
        {
            AddLog($"{L("Main.LogExceptionPrefix")} {ex.Message}");
            _logger.Error("Unexpected error during toggle operation.", ex);
        }
        finally
        {
            await RefreshAsync();
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            var state = await _orchestrator.GetStateAsync();
            ApplyState(state);
        }
        catch (Exception ex)
        {
            AddLog($"{L("Main.LogRefreshFailedPrefix")} {ex.Message}");
            _logger.Error("Refresh failed.", ex);
        }
    }

    private void ApplyState(BlockState state)
    {
        _lastState = state;
        IsBlockActive = state.IsActive;
        _activatedAtUtc = state.ActivatedAt;
        _focusLockUntilUtc = state.FocusLockUntil;
        _unlockPhrase = state.UnlockPhrase;
        _isAdmin = state.IsAdmin;
        _isGuardianHealthy = state.IsGuardianHealthy;
        IsFocusPhraseRequired = state.IsFocusUnlockPhraseRequired;

        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(AdminStatus));
        OnPropertyChanged(nameof(AdminBadgeAppearance));
        OnPropertyChanged(nameof(GuardianStatus));
        OnPropertyChanged(nameof(GuardianBadgeAppearance));
        OnPropertyChanged(nameof(UnlockPhraseDisplay));

        UpdateActiveSinceText();
        UpdateFocusLockText();
        OnPropertyChanged(nameof(FocusProgressPercent));
    }

    private void UpdateActiveSinceText()
    {
        if (!IsBlockActive || _activatedAtUtc is null)
        {
            ActiveSinceText = L("Main.ActiveSinceNone");
            return;
        }

        var localActivated = _activatedAtUtc.Value.ToLocalTime();
        var elapsed = DateTimeOffset.Now - localActivated;
        ActiveSinceText = string.Format(
            CultureInfo.CurrentCulture,
            L("Main.ActiveSinceFormat"),
            localActivated,
            elapsed);
    }

    private void UpdateFocusLockText()
    {
        if (!IsBlockActive || _focusLockUntilUtc is null)
        {
            FocusLockText = L("Main.FocusLockNone");
            return;
        }

        var remaining = _focusLockUntilUtc.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            FocusLockText = L("Main.FocusLockExpired");
            return;
        }

        FocusLockText = string.Format(
            CultureInfo.CurrentCulture,
            L("Main.FocusLockRemainingFormat"),
            remaining,
            IsFocusPhraseRequired ? L("Common.Yes") : L("Common.No"));
    }

    private void AddLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        ActivityLog.Insert(0, line);
        while (ActivityLog.Count > 250)
        {
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }
    }

    private async Task ApplyLanguageSelectionAsync(string languageCode)
    {
        try
        {
            await _localization.SetLanguageAsync(languageCode);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to change app language.", ex);
        }
    }

    private void HandleLanguageChanged(object? sender, EventArgs e)
    {
        _isApplyingLanguageSelection = true;
        SelectedLanguageCode = _localization.CurrentLanguageCode;
        _isApplyingLanguageSelection = false;

        OnPropertyChanged(nameof(ToggleButtonText));
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusBadgeText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(AdminStatus));
        OnPropertyChanged(nameof(GuardianStatus));
        OnPropertyChanged(nameof(UnlockPhraseDisplay));
        OnPropertyChanged(nameof(Localization));

        if (_lastState is not null)
        {
            ApplyState(_lastState);
        }
        else
        {
            UpdateActiveSinceText();
            UpdateFocusLockText();
        }
    }

    private string L(string key)
    {
        return _localization[key];
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
