using System.Collections.ObjectModel;
using System.Windows;
using MouseTune.Commands;
using MouseTune.Models;
using MouseTune.Services;
using ICommand = System.Windows.Input.ICommand;

namespace MouseTune.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private PortableSettings _settings = new();
    private PointerSettings _currentWindowsSettings = PointerSettings.WindowsDefault;
    private DeviceViewModel? _selectedDevice;
    private SavedMouseConfiguration? _selectedSavedConfiguration;
    private string _customName = string.Empty;
    private int _effectiveDpi = EffectiveDpiMapper.DefaultDpi;
    private bool _enhancePointerPrecision;
    private OperationState _state = OperationState.Idle;
    private string _statusMessage = "Ready";
    private AppTheme _selectedTheme = AppTheme.System;
    private CancellationTokenSource? _deviceChangeRefreshCts;

    public MainViewModel(AppServices services)
    {
        _services = services;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ApplyNameCommand = new AsyncRelayCommand(ApplyNameAsync, HasSelectedDevice);
        SaveAndApplyCommand = new AsyncRelayCommand(SaveAndApplyAsync, HasSelectedDevice);
        RestoreDetectedNameCommand = new AsyncRelayCommand(RestoreDetectedNameAsync, HasSelectedDevice);
        ReapplyWindowsNameCommand = new AsyncRelayCommand(ReapplyWindowsNameAsync, HasSelectedDevice);
        RestorePreviousSettingsCommand = new AsyncRelayCommand(RestorePreviousSettingsAsync);
        RestoreWindowsDefaultsCommand = new AsyncRelayCommand(RestoreWindowsDefaultsAsync);
        ExportDiagnosticsCommand = new AsyncRelayCommand(ExportDiagnosticsAsync);
        CopyDiagnosticsSummaryCommand = new RelayCommand(CopyDiagnosticsSummary);
        _ = InitializeAsync(CancellationToken.None);
    }

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();
    public IReadOnlyList<int> DpiPresets { get; } = new[] { 400, 800, 1200, 1600, 2400, 3000, 3200, 4800, 6400 };
    public IReadOnlyList<AppTheme> ThemeOptions { get; } = new[] { AppTheme.System, AppTheme.Light, AppTheme.Dark };

    public ICommand RefreshCommand { get; }
    public ICommand ApplyNameCommand { get; }
    public ICommand SaveAndApplyCommand { get; }
    public ICommand RestoreDetectedNameCommand { get; }
    public ICommand ReapplyWindowsNameCommand { get; }
    public ICommand RestorePreviousSettingsCommand { get; }
    public ICommand RestoreWindowsDefaultsCommand { get; }
    public ICommand ExportDiagnosticsCommand { get; }
    public ICommand CopyDiagnosticsSummaryCommand { get; }

    public DeviceViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                LoadSelectedConfiguration();
                RaiseCommandStates();
            }
        }
    }

    public string CustomName
    {
        get => _customName;
        set => SetProperty(ref _customName, value);
    }

    public int EffectiveDpi
    {
        get => _effectiveDpi;
        set
        {
            if (SetProperty(ref _effectiveDpi, EffectiveDpiMapper.ClampDpi(value)))
            {
                OnPropertyChanged(nameof(WindowsPointerSpeed));
                OnPropertyChanged(nameof(EffectiveDpiDisplay));
                OnPropertyChanged(nameof(SettingsStateText));
            }
        }
    }

    public int WindowsPointerSpeed => EffectiveDpiMapper.ToWindowsSpeed(EffectiveDpi);
    public string EffectiveDpiDisplay => $"{EffectiveDpi} effective DPI";

    public bool EnhancePointerPrecision
    {
        get => _enhancePointerPrecision;
        set
        {
            if (SetProperty(ref _enhancePointerPrecision, value))
            {
                OnPropertyChanged(nameof(SettingsStateText));
            }
        }
    }

    public int CurrentWindowsPointerSpeed => _currentWindowsSettings.WindowsPointerSpeed;
    public string WindowTitleText => Devices.Count > 1 ? "Generic Bluetooth mouse" : "MouseTune";
    public string SelectedDeviceName => SelectedDevice?.CurrentName ?? "No generic mouse detected";
    public string ReportedWindowsName => SelectedDevice?.OriginalName ?? "Not available";
    public string ConnectionTypeText => SelectedDevice?.ConnectionTypeText ?? "Unknown";
    public string ConnectionStatus => SelectedDevice?.ConnectionStatus ?? "Disconnected";
    public bool HasMultipleDevices => Devices.Count > 1;
    public bool HasDevice => SelectedDevice is not null;
    public bool HasSavedAliasDrift =>
        _selectedSavedConfiguration is not null
        && SelectedDevice is not null
        && !string.Equals(_selectedSavedConfiguration.CustomAlias, SelectedDevice.OriginalName, StringComparison.OrdinalIgnoreCase);

    public AppTheme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                _ = SaveThemeAsync(value, CancellationToken.None);
            }
        }
    }

    public string SettingsStateText
    {
        get
        {
            if (_selectedSavedConfiguration is null)
            {
                return "No saved settings for this mouse yet.";
            }

            if (_currentWindowsSettings.WindowsPointerSpeed == _selectedSavedConfiguration.WindowsPointerSpeed
                && _currentWindowsSettings.EnhancePointerPrecision == _selectedSavedConfiguration.EnhancePointerPrecision)
            {
                return "Settings active";
            }

            return "Windows pointer settings have changed since MouseTune last ran";
        }
    }

    public OperationState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _settings = await _services.PortableSettings.LoadAsync(cancellationToken).ConfigureAwait(true);
            _selectedTheme = _settings.Theme;
            _services.Theme.Apply(_settings.Theme);
            OnPropertyChanged(nameof(SelectedTheme));
            _currentWindowsSettings = _services.PointerSettings.ReadCurrent();
            await _services.PortableSettings
                .CaptureOriginalWindowsSettingsIfNeededAsync(_settings, _currentWindowsSettings, cancellationToken)
                .ConfigureAwait(true);
            await RefreshAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            State = OperationState.Failed;
            StatusMessage = $"Startup failed: {ex.Message}";
            _services.Logger.Log("StartupFailed", "Startup failed.", ex);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        State = OperationState.Scanning;
        StatusMessage = "Scanning for generic mouse devices...";

        try
        {
            _settings = await _services.PortableSettings.LoadAsync(cancellationToken).ConfigureAwait(true);
            _currentWindowsSettings = _services.PointerSettings.ReadCurrent();
            var previousStableId = SelectedDevice?.Model.StableId;
            var devices = await _services.Discovery.GetMouseDevicesAsync(includeVirtualDevices: false, cancellationToken)
                .ConfigureAwait(true);

            Devices.Clear();
            foreach (var device in devices.Where(IsGenericMouse))
            {
                var saved = _services.DeviceIdentity.FindBestMatch(device, _settings.SavedDevices);
                if (saved is not null)
                {
                    device.CurrentName = saved.CustomAlias;
                }

                Devices.Add(new DeviceViewModel(device));
            }

            SelectedDevice = Devices.FirstOrDefault(device => string.Equals(device.Model.StableId, previousStableId, StringComparison.OrdinalIgnoreCase))
                ?? Devices.FirstOrDefault(device => device.Model.IsConnected)
                ?? Devices.FirstOrDefault();

            State = OperationState.Successful;
            StatusMessage = Devices.Count == 0
                ? "No generic Bluetooth mouse detected. Connect a mouse, then refresh."
                : $"Detected {Devices.Count} generic mouse device(s).";
            RefreshComputedProperties();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            State = OperationState.Failed;
            StatusMessage = ex.Message;
            _services.Logger.Log("PortableRefreshFailed", "Portable refresh failed.", ex);
        }
    }

    public void QueueDeviceChangeRefresh()
    {
        _deviceChangeRefreshCts?.Cancel();
        _deviceChangeRefreshCts?.Dispose();
        _deviceChangeRefreshCts = new CancellationTokenSource();
        _ = RefreshAfterDeviceChangeAsync(_deviceChangeRefreshCts.Token);
    }

    private async Task RefreshAfterDeviceChangeAsync(CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = "Device change detected. Refreshing...";
            await Task.Delay(600, cancellationToken).ConfigureAwait(true);
            await RefreshAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ApplyNameAsync(CancellationToken cancellationToken)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        var validation = DeviceNameValidator.Validate(CustomName);
        if (!validation.Succeeded)
        {
            State = OperationState.Failed;
            StatusMessage = validation.Message;
            return;
        }

        State = OperationState.Applying;
        var rename = await _services.Rename.ApplyNameAsync(SelectedDevice.Model, CustomName, cancellationToken).ConfigureAwait(true);
        SelectedDevice.CurrentName = DeviceNameValidator.Normalize(CustomName);
        await SaveSelectedConfigurationAsync(cancellationToken).ConfigureAwait(true);
        State = rename.Succeeded ? OperationState.Successful : OperationState.Failed;
        StatusMessage = rename.Message;
        RefreshComputedProperties();
    }

    private async Task SaveAndApplyAsync(CancellationToken cancellationToken)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        var validation = DeviceNameValidator.Validate(CustomName);
        if (!validation.Succeeded)
        {
            State = OperationState.Failed;
            StatusMessage = validation.Message;
            return;
        }

        State = OperationState.Applying;
        var pointerResult = _services.PointerSettings.ApplyEffectiveDpiVerified(EffectiveDpi, EnhancePointerPrecision);
        var renameResult = await _services.Rename.ApplyNameAsync(SelectedDevice.Model, CustomName, cancellationToken).ConfigureAwait(true);

        if (pointerResult.Succeeded)
        {
            _currentWindowsSettings = _services.PointerSettings.ReadCurrent();
            SelectedDevice.CurrentName = DeviceNameValidator.Normalize(CustomName);
            await SaveSelectedConfigurationAsync(cancellationToken).ConfigureAwait(true);
            State = OperationState.Successful;
            StatusMessage = renameResult.Succeeded
                ? "Saved successfully. The Windows pointer settings will remain active after MouseTune closes."
                : $"Sensitivity saved, but name change was partial: {renameResult.Message}";
        }
        else
        {
            State = OperationState.Failed;
            StatusMessage = pointerResult.Message;
        }

        RefreshComputedProperties();
    }

    private async Task RestoreDetectedNameAsync(CancellationToken cancellationToken)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        CustomName = SelectedDevice.OriginalName;
        SelectedDevice.CurrentName = SelectedDevice.OriginalName;
        _selectedSavedConfiguration = _services.DeviceIdentity.FindBestMatch(SelectedDevice.Model, _settings.SavedDevices);
        if (_selectedSavedConfiguration is not null)
        {
            _selectedSavedConfiguration.CustomAlias = SelectedDevice.OriginalName;
            _selectedSavedConfiguration.LastModifiedUtc = DateTimeOffset.UtcNow;
            await _services.PortableSettings.SaveAsync(_settings, cancellationToken).ConfigureAwait(true);
        }

        var result = await _services.Rename.RestoreOriginalAsync(SelectedDevice.Model, cancellationToken).ConfigureAwait(true);
        State = result.Succeeded ? OperationState.Successful : OperationState.Failed;
        StatusMessage = result.Message;
        RefreshComputedProperties();
    }

    private async Task ReapplyWindowsNameAsync(CancellationToken cancellationToken)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        var alias = _selectedSavedConfiguration?.CustomAlias ?? CustomName;
        CustomName = alias;
        await ApplyNameAsync(cancellationToken).ConfigureAwait(true);
    }

    private Task RestorePreviousSettingsAsync(CancellationToken cancellationToken)
    {
        if (!ConfirmRestore("Restore the Windows mouse settings captured before MouseTune first changed them?"))
        {
            return Task.CompletedTask;
        }

        if (_settings.OriginalWindowsSettings is null)
        {
            StatusMessage = "No pre-MouseTune Windows settings have been captured yet.";
            return Task.CompletedTask;
        }

        var result = _services.PointerSettings.RestoreSnapshot(_settings.OriginalWindowsSettings);
        _currentWindowsSettings = _services.PointerSettings.ReadCurrent();
        State = result.Succeeded ? OperationState.Successful : OperationState.Failed;
        StatusMessage = result.Message;
        RefreshComputedProperties();
        return Task.CompletedTask;
    }

    private Task RestoreWindowsDefaultsAsync(CancellationToken cancellationToken)
    {
        if (!ConfirmRestore("Restore standard Windows mouse defaults?"))
        {
            return Task.CompletedTask;
        }

        var result = _services.PointerSettings.ResetToWindowsDefault();
        _currentWindowsSettings = _services.PointerSettings.ReadCurrent();
        State = result.Succeeded ? OperationState.Successful : OperationState.Failed;
        StatusMessage = result.Message;
        RefreshComputedProperties();
        return Task.CompletedTask;
    }

    private async Task ExportDiagnosticsAsync(CancellationToken cancellationToken)
    {
        State = OperationState.Applying;

        try
        {
            var path = await _services.Diagnostics
                .ExportAsync(_settings, _currentWindowsSettings, Devices.Select(device => device.Model), cancellationToken)
                .ConfigureAwait(true);
            State = OperationState.Successful;
            StatusMessage = $"Diagnostics exported to {path}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            State = OperationState.Failed;
            StatusMessage = $"Diagnostics export failed: {ex.Message}";
            _services.Logger.Log("DiagnosticsExportFailed", "Diagnostics export failed.", ex);
        }
    }

    private void CopyDiagnosticsSummary()
    {
        try
        {
            var summary = _services.Diagnostics.CreateSummary(_settings, _currentWindowsSettings, Devices.Select(device => device.Model));
            Clipboard.SetText(summary);
            State = OperationState.Successful;
            StatusMessage = "Diagnostics summary copied to clipboard.";
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            State = OperationState.Failed;
            StatusMessage = $"Could not copy diagnostics summary: {ex.Message}";
            _services.Logger.Log("DiagnosticsCopyFailed", "Diagnostics copy failed.", ex);
        }
    }

    private async Task SaveThemeAsync(AppTheme theme, CancellationToken cancellationToken)
    {
        try
        {
            _settings.Theme = theme;
            _services.Theme.Apply(theme);
            await _services.PortableSettings.SaveAsync(_settings, cancellationToken).ConfigureAwait(true);
            StatusMessage = $"Theme set to {theme}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            State = OperationState.Failed;
            StatusMessage = $"Theme could not be saved: {ex.Message}";
            _services.Logger.Log("ThemeSaveFailed", "Theme save failed.", ex);
        }
    }

    private void LoadSelectedConfiguration()
    {
        if (SelectedDevice is null)
        {
            _selectedSavedConfiguration = null;
            CustomName = string.Empty;
            EffectiveDpi = EffectiveDpiMapper.ToEffectiveDpi(_currentWindowsSettings.WindowsPointerSpeed);
            EnhancePointerPrecision = _currentWindowsSettings.EnhancePointerPrecision;
            RefreshComputedProperties();
            return;
        }

        _selectedSavedConfiguration = _services.DeviceIdentity.FindBestMatch(SelectedDevice.Model, _settings.SavedDevices);
        if (_selectedSavedConfiguration is null)
        {
            CustomName = SelectedDevice.OriginalName;
            EffectiveDpi = EffectiveDpiMapper.ToEffectiveDpi(_currentWindowsSettings.WindowsPointerSpeed);
            EnhancePointerPrecision = _currentWindowsSettings.EnhancePointerPrecision;
        }
        else
        {
            CustomName = _selectedSavedConfiguration.CustomAlias;
            EffectiveDpi = _selectedSavedConfiguration.EffectiveDpi;
            EnhancePointerPrecision = _selectedSavedConfiguration.EnhancePointerPrecision;
            SelectedDevice.CurrentName = _selectedSavedConfiguration.CustomAlias;
        }

        RefreshComputedProperties();
    }

    private async Task SaveSelectedConfigurationAsync(CancellationToken cancellationToken)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        var updated = _services.DeviceIdentity.CreateConfiguration(
            SelectedDevice.Model,
            CustomName,
            EffectiveDpi,
            WindowsPointerSpeed,
            EnhancePointerPrecision);

        var existing = _services.DeviceIdentity.FindBestMatch(SelectedDevice.Model, _settings.SavedDevices);
        if (existing is null)
        {
            _settings.SavedDevices.Add(updated);
            _selectedSavedConfiguration = updated;
        }
        else
        {
            var index = _settings.SavedDevices.IndexOf(existing);
            _settings.SavedDevices[index] = updated;
            _selectedSavedConfiguration = updated;
        }

        await _services.PortableSettings.SaveAsync(_settings, cancellationToken).ConfigureAwait(true);
    }

    private bool HasSelectedDevice() => SelectedDevice is not null;

    private static bool IsGenericMouse(MouseDevice device)
    {
        if (device.ConnectionType is MouseConnectionType.Bluetooth or MouseConnectionType.BluetoothLe)
        {
            return true;
        }

        var name = $"{device.CurrentName} {device.OriginalName} {device.Manufacturer}";
        return name.Contains("generic", StringComparison.OrdinalIgnoreCase)
            || name.Contains("bluetooth", StringComparison.OrdinalIgnoreCase)
            || name.Contains("wireless", StringComparison.OrdinalIgnoreCase)
            || name.Contains("BT ", StringComparison.OrdinalIgnoreCase)
            || name.Contains("BLE", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(CurrentWindowsPointerSpeed));
        OnPropertyChanged(nameof(WindowTitleText));
        OnPropertyChanged(nameof(SelectedDeviceName));
        OnPropertyChanged(nameof(ReportedWindowsName));
        OnPropertyChanged(nameof(ConnectionTypeText));
        OnPropertyChanged(nameof(ConnectionStatus));
        OnPropertyChanged(nameof(HasMultipleDevices));
        OnPropertyChanged(nameof(HasDevice));
        OnPropertyChanged(nameof(HasSavedAliasDrift));
        OnPropertyChanged(nameof(SettingsStateText));
    }

    private void RaiseCommandStates()
    {
        (ApplyNameCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SaveAndApplyCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RestoreDetectedNameCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ReapplyWindowsNameCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private static bool ConfirmRestore(string message) =>
        MessageBox.Show(
            message,
            "Confirm restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
