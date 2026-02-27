using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Blocker.App.Contracts;

namespace Blocker.App.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string PolishLanguageCode = "pl";
    private const string EnglishLanguageCode = "en";

    private static readonly IReadOnlyDictionary<string, string> PolishStrings = new Dictionary<string, string>
    {
        ["Common.Yes"] = "TAK",
        ["Common.No"] = "NIE",
        ["Common.Cancel"] = "Anuluj",
        ["Main.FocusDashboardHeader"] = "Panel skupienia",
        ["Main.HeroTitle"] = "Skupienie bez rozpraszaczy",
        ["Main.HeroDescription"] = "Discord i Messenger/Facebook są blokowane na poziomie aplikacji i webu. Reszta systemu pozostaje dostępna.",
        ["Main.StatusControlHeader"] = "Stan i sterowanie",
        ["Main.AdminLabel"] = "Administrator",
        ["Main.GuardianLabel"] = "Guardian",
        ["Main.ModeLabel"] = "Tryb",
        ["Main.RefreshStatus"] = "Odśwież status",
        ["Main.FocusLockHeader"] = "Focus lock",
        ["Main.FocusLockDescription"] = "W trakcie 30 minutowego locka, wyłączenie wymaga wpisania frazy potwierdzającej.",
        ["Main.FocusPhraseRequiredTitle"] = "Fraza wymagana",
        ["Main.FocusPhraseRequiredMessage"] = "Aby zakończyć sesję przed czasem, wpisz frazę zabezpieczającą.",
        ["Main.EventLogHeader"] = "Dziennik zdarzeń",
        ["Main.LanguageLabel"] = "Język",
        ["Main.LanguagePolish"] = "Polski",
        ["Main.LanguageEnglish"] = "English",
        ["Main.PhraseLabel"] = "Fraza:",
        ["Main.ToggleOn"] = "Włącz blokadę",
        ["Main.ToggleOff"] = "Wyłącz blokadę",
        ["Main.StatusTitleActive"] = "Blokada aktywna",
        ["Main.StatusTitleInactive"] = "Blokada nieaktywna",
        ["Main.StatusBadgeOn"] = "TRYB: ON",
        ["Main.StatusBadgeOff"] = "TRYB: OFF",
        ["Main.StatusActive"] = "Discord i Messenger/Facebook są blokowane.",
        ["Main.StatusInactive"] = "Ograniczenia są wyłączone.",
        ["Main.GuardianActive"] = "AKTYWNY",
        ["Main.GuardianUnavailable"] = "NIEDOSTĘPNY",
        ["Main.NotApplicable"] = "NIE DOTYCZY",
        ["Main.ActiveSinceNone"] = "Aktywna od: -",
        ["Main.ActiveSinceFormat"] = "Aktywna od: {0:yyyy-MM-dd HH:mm:ss} ({1:hh\\:mm\\:ss})",
        ["Main.FocusLockNone"] = "Focus lock: -",
        ["Main.FocusLockExpired"] = "Focus lock wygasł. Możesz wyłączyć blokadę bez frazy.",
        ["Main.FocusLockRemainingFormat"] = "Pozostało: {0:mm\\:ss} | fraza wymagana: {1}",
        ["Main.LogAppStarted"] = "Aplikacja uruchomiona.",
        ["Main.LogEnabling"] = "Włączanie blokady...",
        ["Main.LogDisabling"] = "Wyłączanie blokady...",
        ["Main.LogNoSetupDialog"] = "Brak okna konfiguracji frazy. Nie można włączyć blokady.",
        ["Main.LogEnableCancelled"] = "Włączenie anulowane.",
        ["Main.LogDisableCancelled"] = "Wyłączenie anulowane.",
        ["Main.LogErrorPrefix"] = "Błąd:",
        ["Main.LogOperationWithErrors"] = "Operacja zakończona z błędami.",
        ["Main.LogExceptionPrefix"] = "Wyjątek:",
        ["Main.LogRefreshFailedPrefix"] = "Nie udało się odświeżyć statusu:",
        ["Unlock.EmptyPhraseWarning"] = "Fraza nie może być pusta.",
        ["Unlock.SetupWindowTitle"] = "Ustaw frazę",
        ["Unlock.SetupTitleBar"] = "Ustaw frazę Focus Lock",
        ["Unlock.SetupSectionTitle"] = "Ustaw frazę do wczesnego odblokowania",
        ["Unlock.SetupMessageTitle"] = "Nowa sesja",
        ["Unlock.SetupMessageBody"] = "Przed włączeniem blokady ustaw frazę. Będzie wymagana do wyłączenia przed upływem 30 minut.",
        ["Unlock.SetupPlaceholder"] = "Ustaw frazę...",
        ["Unlock.SetupConfirm"] = "Ustaw i włącz",
        ["Unlock.VerifyWindowTitle"] = "Potwierdzenie",
        ["Unlock.VerifyTitleBar"] = "Potwierdzenie Focus Lock",
        ["Unlock.VerifySectionTitle"] = "Wpisz frazę potwierdzającą",
        ["Unlock.VerifyMessageTitle"] = "Wczesne zakończenie sesji",
        ["Unlock.VerifyMessageBody"] = "Aby zakończyć blokadę przed upływem 30 minut, wpisz dokładnie wymaganą frazę.",
        ["Unlock.VerifyPlaceholder"] = "Wpisz frazę...",
        ["Unlock.VerifyConfirm"] = "Potwierdź i wyłącz",
        ["App.StartupFailed"] = "Nie udało się uruchomić aplikacji:",
        ["App.DisableBeforeExit"] = "Najpierw wyłącz blokadę, aby zamknąć aplikację.",
        ["App.UnexpectedError"] = "Wystąpił nieoczekiwany błąd:",
        ["Tray.OpenWindow"] = "Pokaż okno",
        ["Tray.EnableBlock"] = "Włącz blokadę",
        ["Tray.DisableBlock"] = "Wyłącz blokadę",
        ["Tray.Exit"] = "Zamknij",
        ["Tray.StatusOn"] = "Blocker (WŁ.)",
        ["Tray.StatusOff"] = "Blocker (WYŁ.)"
    };

    private static readonly IReadOnlyDictionary<string, string> EnglishStrings = new Dictionary<string, string>
    {
        ["Common.Yes"] = "YES",
        ["Common.No"] = "NO",
        ["Common.Cancel"] = "Cancel",
        ["Main.FocusDashboardHeader"] = "Focus Dashboard",
        ["Main.HeroTitle"] = "Focus without distractions",
        ["Main.HeroDescription"] = "Discord and Messenger/Facebook are blocked at app and web level. The rest of the system remains available.",
        ["Main.StatusControlHeader"] = "Status and controls",
        ["Main.AdminLabel"] = "Administrator",
        ["Main.GuardianLabel"] = "Guardian",
        ["Main.ModeLabel"] = "Mode",
        ["Main.RefreshStatus"] = "Refresh status",
        ["Main.FocusLockHeader"] = "Focus lock",
        ["Main.FocusLockDescription"] = "During the 30-minute lock, disabling requires entering the confirmation phrase.",
        ["Main.FocusPhraseRequiredTitle"] = "Phrase required",
        ["Main.FocusPhraseRequiredMessage"] = "To end the session early, enter the security phrase.",
        ["Main.EventLogHeader"] = "Event log",
        ["Main.LanguageLabel"] = "Language",
        ["Main.LanguagePolish"] = "Polski",
        ["Main.LanguageEnglish"] = "English",
        ["Main.PhraseLabel"] = "Phrase:",
        ["Main.ToggleOn"] = "Enable blocking",
        ["Main.ToggleOff"] = "Disable blocking",
        ["Main.StatusTitleActive"] = "Blocking active",
        ["Main.StatusTitleInactive"] = "Blocking inactive",
        ["Main.StatusBadgeOn"] = "MODE: ON",
        ["Main.StatusBadgeOff"] = "MODE: OFF",
        ["Main.StatusActive"] = "Discord and Messenger/Facebook are blocked.",
        ["Main.StatusInactive"] = "Restrictions are disabled.",
        ["Main.GuardianActive"] = "ACTIVE",
        ["Main.GuardianUnavailable"] = "UNAVAILABLE",
        ["Main.NotApplicable"] = "N/A",
        ["Main.ActiveSinceNone"] = "Active since: -",
        ["Main.ActiveSinceFormat"] = "Active since: {0:yyyy-MM-dd HH:mm:ss} ({1:hh\\:mm\\:ss})",
        ["Main.FocusLockNone"] = "Focus lock: -",
        ["Main.FocusLockExpired"] = "Focus lock expired. You can disable blocking without a phrase.",
        ["Main.FocusLockRemainingFormat"] = "Remaining: {0:mm\\:ss} | phrase required: {1}",
        ["Main.LogAppStarted"] = "Application started.",
        ["Main.LogEnabling"] = "Enabling blocking...",
        ["Main.LogDisabling"] = "Disabling blocking...",
        ["Main.LogNoSetupDialog"] = "Phrase setup dialog is unavailable. Cannot enable blocking.",
        ["Main.LogEnableCancelled"] = "Enable canceled.",
        ["Main.LogDisableCancelled"] = "Disable canceled.",
        ["Main.LogErrorPrefix"] = "Error:",
        ["Main.LogOperationWithErrors"] = "Operation completed with errors.",
        ["Main.LogExceptionPrefix"] = "Exception:",
        ["Main.LogRefreshFailedPrefix"] = "Failed to refresh status:",
        ["Unlock.EmptyPhraseWarning"] = "Phrase cannot be empty.",
        ["Unlock.SetupWindowTitle"] = "Set phrase",
        ["Unlock.SetupTitleBar"] = "Set Focus Lock phrase",
        ["Unlock.SetupSectionTitle"] = "Set phrase for early unlock",
        ["Unlock.SetupMessageTitle"] = "New session",
        ["Unlock.SetupMessageBody"] = "Before enabling blocking, set a phrase. It will be required to disable before 30 minutes.",
        ["Unlock.SetupPlaceholder"] = "Set phrase...",
        ["Unlock.SetupConfirm"] = "Set and enable",
        ["Unlock.VerifyWindowTitle"] = "Confirmation",
        ["Unlock.VerifyTitleBar"] = "Focus Lock confirmation",
        ["Unlock.VerifySectionTitle"] = "Enter confirmation phrase",
        ["Unlock.VerifyMessageTitle"] = "Early session end",
        ["Unlock.VerifyMessageBody"] = "To disable blocking before 30 minutes, enter the required phrase exactly.",
        ["Unlock.VerifyPlaceholder"] = "Enter phrase...",
        ["Unlock.VerifyConfirm"] = "Confirm and disable",
        ["App.StartupFailed"] = "Application startup failed:",
        ["App.DisableBeforeExit"] = "Disable blocking before closing the app.",
        ["App.UnexpectedError"] = "Unexpected error occurred:",
        ["Tray.OpenWindow"] = "Open window",
        ["Tray.EnableBlock"] = "Enable blocking",
        ["Tray.DisableBlock"] = "Disable blocking",
        ["Tray.Exit"] = "Exit",
        ["Tray.StatusOn"] = "Blocker (ON)",
        ["Tray.StatusOff"] = "Blocker (OFF)"
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogService _logger;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };
    private string _currentLanguageCode = EnglishLanguageCode;

    public LocalizationService(ILogService logger)
    {
        _logger = logger;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Blocker");
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "localization.json");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public string CurrentLanguageCode => _currentLanguageCode;

    public string this[string key] => GetString(key);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var settings = await LoadSettingsUnsafeAsync(cancellationToken);
            var loaded = NormalizeLanguageCode(settings?.LanguageCode);

            if (loaded is null)
            {
                loaded = DetectSystemLanguageCode();
                await SaveSettingsUnsafeAsync(new LocalizationSettings
                {
                    LanguageCode = loaded,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                }, cancellationToken);
            }

            _currentLanguageCode = loaded;
            OnPropertyChanged(nameof(CurrentLanguageCode));
            OnPropertyChanged("Item[]");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLanguageCode(languageCode) ?? EnglishLanguageCode;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(_currentLanguageCode, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentLanguageCode = normalized;
            await SaveSettingsUnsafeAsync(new LocalizationSettings
            {
                LanguageCode = _currentLanguageCode,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        OnPropertyChanged(nameof(CurrentLanguageCode));
        OnPropertyChanged("Item[]");
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetString(string key)
    {
        var source = string.Equals(_currentLanguageCode, PolishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? PolishStrings
            : EnglishStrings;

        if (source.TryGetValue(key, out var value))
        {
            return value;
        }

        if (EnglishStrings.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    private static string DetectSystemLanguageCode()
    {
        var systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return string.Equals(systemLanguage, PolishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? PolishLanguageCode
            : EnglishLanguageCode;
    }

    private static string? NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var normalized = languageCode.Trim().ToLowerInvariant();
        return normalized switch
        {
            PolishLanguageCode => PolishLanguageCode,
            EnglishLanguageCode => EnglishLanguageCode,
            _ => null
        };
    }

    private async Task<LocalizationSettings?> LoadSettingsUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(_settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<LocalizationSettings>(stream, _serializerOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to load localization settings. Falling back to system language. {ex.Message}");
            return null;
        }
    }

    private async Task SaveSettingsUnsafeAsync(LocalizationSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var json = JsonSerializer.Serialize(settings, _serializerOptions);
            await File.WriteAllTextAsync(_settingsPath, json, new UTF8Encoding(false), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to save localization settings. {ex.Message}");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class LocalizationSettings
    {
        public string? LanguageCode { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
