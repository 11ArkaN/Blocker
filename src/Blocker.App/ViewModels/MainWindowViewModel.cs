using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private readonly RelayCommand _toggleCommand;
    private readonly RelayCommand _refreshCommand;
    private readonly DispatcherTimer _uptimeTimer;

    private bool _isBlockActive;
    private bool _isBusy;
    private bool _isFocusPhraseRequired;
    private DateTimeOffset? _activatedAtUtc;
    private DateTimeOffset? _focusLockUntilUtc;
    private string _statusText = "Blokada jest wylaczona.";
    private string _adminStatus = "NIE";
    private string _guardianStatus = "NIEZNANY";
    private string _activeSinceText = "Aktywna od: -";
    private string _focusLockText = "Focus lock: -";
    private string? _unlockPhrase;
    private string _unlockPhraseDisplay = "Fraza: -";

    public MainWindowViewModel(IBlockOrchestrator orchestrator, ILogService logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;

        _toggleCommand = new RelayCommand(_ => _ = ToggleBlockAsync(), _ => !IsBusy);
        _refreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsBusy);

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

    public Func<string?, Task<string?>>? RequestUnlockPhraseAsync { get; set; }
    public Func<Task<string?>>? RequestSetupUnlockPhraseAsync { get; set; }

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
            OnPropertyChanged(nameof(StatusSeverity));
            OnPropertyChanged(nameof(ModeBadgeAppearance));
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

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string AdminStatus
    {
        get => _adminStatus;
        private set
        {
            if (_adminStatus == value)
            {
                return;
            }

            _adminStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AdminBadgeAppearance));
        }
    }

    public string GuardianStatus
    {
        get => _guardianStatus;
        private set
        {
            if (_guardianStatus == value)
            {
                return;
            }

            _guardianStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GuardianBadgeAppearance));
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
        get => _unlockPhraseDisplay;
        private set
        {
            if (_unlockPhraseDisplay == value)
            {
                return;
            }

            _unlockPhraseDisplay = value;
            OnPropertyChanged();
        }
    }

    public string ToggleButtonText => IsBlockActive ? "Wylacz blokade" : "Wlacz blokade";

    public string StatusTitle => IsBlockActive ? "Blokada aktywna" : "Blokada nieaktywna";

    public string StatusBadgeText => IsBlockActive ? "TRYB: ON" : "TRYB: OFF";

    public ControlAppearance ToggleButtonAppearance => IsBlockActive ? ControlAppearance.Danger : ControlAppearance.Success;

    public InfoBarSeverity StatusSeverity => IsBlockActive ? InfoBarSeverity.Success : InfoBarSeverity.Informational;

    public ControlAppearance ModeBadgeAppearance => IsBlockActive ? ControlAppearance.Success : ControlAppearance.Secondary;

    public ControlAppearance AdminBadgeAppearance => AdminStatus == "TAK" ? ControlAppearance.Success : ControlAppearance.Danger;

    public ControlAppearance GuardianBadgeAppearance
    {
        get
        {
            if (!IsBlockActive)
            {
                return ControlAppearance.Secondary;
            }

            return GuardianStatus == "AKTYWNY" ? ControlAppearance.Success : ControlAppearance.Danger;
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
        AddLog("Aplikacja uruchomiona.");
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
        AddLog(enable ? "Wlaczanie blokady..." : "Wylaczanie blokady...");

        try
        {
            BlockResult result;
            if (enable)
            {
                if (RequestSetupUnlockPhraseAsync is null)
                {
                    AddLog("Brak okna konfiguracji frazy. Nie mozna wlaczyc blokady.");
                    return;
                }

                var newPhrase = await RequestSetupUnlockPhraseAsync();
                if (newPhrase is null)
                {
                    AddLog("Wlaczenie anulowane.");
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
                        AddLog("Wylaczenie anulowane.");
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
                AddLog($"Blad: {error}");
            }

            if (!result.Success)
            {
                AddLog("Operacja zakonczona z bledami.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"Wyjatek: {ex.Message}");
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
            AddLog($"Nie udalo sie odswiezyc statusu: {ex.Message}");
            _logger.Error("Refresh failed.", ex);
        }
    }

    private void ApplyState(BlockState state)
    {
        IsBlockActive = state.IsActive;
        _activatedAtUtc = state.ActivatedAt;
        _focusLockUntilUtc = state.FocusLockUntil;
        _unlockPhrase = state.UnlockPhrase;
        IsFocusPhraseRequired = state.IsFocusUnlockPhraseRequired;
        UnlockPhraseDisplay = !string.IsNullOrWhiteSpace(_unlockPhrase) ? $"Fraza: {_unlockPhrase}" : "Fraza: -";

        StatusText = state.IsActive
            ? "Discord i Messenger/Facebook sa blokowane."
            : "Ograniczenia sa wylaczone.";
        AdminStatus = state.IsAdmin ? "TAK" : "NIE";
        GuardianStatus = state.IsActive ? (state.IsGuardianHealthy ? "AKTYWNY" : "NIEDOSTEPNY") : "NIE DOTYCZY";

        UpdateActiveSinceText();
        UpdateFocusLockText();
        OnPropertyChanged(nameof(FocusProgressPercent));
    }

    private void UpdateActiveSinceText()
    {
        if (!IsBlockActive || _activatedAtUtc is null)
        {
            ActiveSinceText = "Aktywna od: -";
            return;
        }

        var localActivated = _activatedAtUtc.Value.ToLocalTime();
        var elapsed = DateTimeOffset.Now - localActivated;
        ActiveSinceText = $"Aktywna od: {localActivated:yyyy-MM-dd HH:mm:ss} ({elapsed:hh\\:mm\\:ss})";
    }

    private void UpdateFocusLockText()
    {
        if (!IsBlockActive || _focusLockUntilUtc is null)
        {
            FocusLockText = "Focus lock: -";
            return;
        }

        var remaining = _focusLockUntilUtc.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            FocusLockText = "Focus lock wygasl. Mozesz wylaczyc blokade bez frazy.";
            return;
        }

        FocusLockText = $"Pozostalo: {remaining:mm\\:ss} | fraza wymagana: {(IsFocusPhraseRequired ? "TAK" : "NIE")}";
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
