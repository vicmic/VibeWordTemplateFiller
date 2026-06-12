using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistanceCalculator.Models;
using DistanceCalculator.Services;
using Microsoft.Win32;

namespace DistanceCalculator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GoogleMapsService _googleMaps = new();
    private readonly ExcelService _excelService = new();
    private CancellationTokenSource? _cts;
    private ExcelLoadResult? _loadResult;
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DistanceCalculator", "settings.json");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCalculate))]
    private string _apiKey = "";

    [ObservableProperty]
    private bool _isApiKeyValid;

    [ObservableProperty]
    private string _apiKeyStatus = "";

    [ObservableProperty]
    private bool _isValidatingKey;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFileLoaded))]
    [NotifyPropertyChangedFor(nameof(CanCalculate))]
    private string? _filePath;

    [ObservableProperty]
    private string _fileName = "Nessun file selezionato";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCalculate))]
    private bool _isProcessing;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = "Pronto";

    [ObservableProperty]
    private string _processedInfo = "";

    [ObservableProperty]
    private double _totalDistanceKm;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _saveSuccess;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCalculate))]
    private int _departureColumnIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCalculate))]
    private int _arrivalColumnIndex = 1;

    [ObservableProperty]
    private int _depDescColumnIndex = -1;

    [ObservableProperty]
    private int _arrDescColumnIndex = -1;

    [ObservableProperty]
    private bool _hasHeaderRow = true;

    public ObservableCollection<AddressRow> Rows { get; } = [];
    public ObservableCollection<ColumnOption> ColumnOptions { get; } = [];
    public ObservableCollection<ColumnOption> OptionalColumnOptions { get; } = [];

    public bool IsFileLoaded => FilePath != null;
    public bool CanCalculate => IsFileLoaded && !string.IsNullOrWhiteSpace(ApiKey) && !IsProcessing;

    public MainViewModel()
    {
        LoadSettings();
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona file Excel",
            Filter = "File Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tutti i file (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == true)
            LoadFile(dialog.FileName);
    }

    public void LoadFile(string path)
    {
        var result = _excelService.LoadFile(path);
        if (!result.Success)
        {
            StatusMessage = $"Errore: {result.Error}";
            return;
        }

        _loadResult = result;
        FilePath = path;
        FileName = Path.GetFileName(path);
        HasHeaderRow = result.HasHeaders;

        BuildColumnOptions(result);
        var (dep, arr, depDesc, arrDesc) = _excelService.AutoDetectColumns(result.Headers);
        DepartureColumnIndex = dep;
        ArrivalColumnIndex = arr;
        DepDescColumnIndex = depDesc;
        ArrDescColumnIndex = arrDesc;

        RefreshRows();
        StatusMessage = $"File caricato: {result.RawData.Count} righe trovate";
        HasResults = false;
        SaveSuccess = false;
    }

    private void BuildColumnOptions(ExcelLoadResult result)
    {
        ColumnOptions.Clear();
        OptionalColumnOptions.Clear();

        OptionalColumnOptions.Add(new ColumnOption { Index = -1, DisplayName = "-- Nessuno --" });

        for (int i = 0; i < result.ColumnCount; i++)
        {
            string colLetter = GetColumnLetter(i);
            string header = i < result.Headers.Count ? result.Headers[i] : "";
            string label = string.IsNullOrWhiteSpace(header)
                ? $"Colonna {colLetter}"
                : $"{colLetter}: {header}";

            var opt = new ColumnOption { Index = i, DisplayName = label };
            ColumnOptions.Add(opt);
            OptionalColumnOptions.Add(new ColumnOption { Index = i, DisplayName = label });
        }
    }

    partial void OnDepartureColumnIndexChanged(int value) => RefreshRows();
    partial void OnArrivalColumnIndexChanged(int value) => RefreshRows();
    partial void OnDepDescColumnIndexChanged(int value) => RefreshRows();
    partial void OnArrDescColumnIndexChanged(int value) => RefreshRows();
    partial void OnHasHeaderRowChanged(bool value) => RefreshRows();

    private void RefreshRows()
    {
        if (_loadResult is null) return;

        var config = BuildConfig();
        var rows = _excelService.BuildRows(_loadResult, config);
        Rows.Clear();
        foreach (var r in rows) Rows.Add(r);
        ProcessedInfo = $"{Rows.Count} righe da elaborare";
    }

    [RelayCommand]
    private async Task CalculateAsync()
    {
        if (_loadResult is null) return;

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        HasResults = false;
        SaveSuccess = false;
        Progress = 0;
        TotalDistanceKm = 0;

        var config = BuildConfig();
        var processable = Rows.Where(r => r.Status != ProcessingStatus.Skipped).ToList();
        int total = processable.Count;
        int done = 0;
        double totalKm = 0;

        foreach (var row in Rows)
        {
            if (row.Status == ProcessingStatus.Skipped) continue;

            if (_cts.Token.IsCancellationRequested)
            {
                row.Status = ProcessingStatus.Skipped;
                row.ErrorMessage = "Annullato dall'utente";
                continue;
            }

            row.Status = ProcessingStatus.Processing;
            StatusMessage = $"Elaborazione riga {row.RowNumber}: {row.DepartureAddress} → {row.ArrivalAddress}";

            try
            {
                await Task.Delay(120, _cts.Token);
                var (km, error) = await _googleMaps.GetDistanceAsync(
                    row.DepartureAddress, row.ArrivalAddress, ApiKey, _cts.Token);

                if (error is null && km.HasValue)
                {
                    row.DistanceKm = km;
                    row.Status = ProcessingStatus.Success;
                    totalKm += km.Value;
                }
                else
                {
                    row.ErrorMessage = error;
                    row.Status = ProcessingStatus.Error;
                }
            }
            catch (OperationCanceledException)
            {
                row.Status = ProcessingStatus.Skipped;
                row.ErrorMessage = "Annullato";
            }
            catch (Exception ex)
            {
                row.ErrorMessage = ex.Message;
                row.Status = ProcessingStatus.Error;
            }

            done++;
            Progress = (double)done / total * 100;
            ProcessedInfo = $"{done}/{total} righe elaborate";
        }

        TotalDistanceKm = totalKm;
        IsProcessing = false;
        HasResults = true;
        StatusMessage = _cts.IsCancellationRequested
            ? $"Elaborazione annullata — {done}/{total} righe completate, {totalKm:F2} km totali"
            : $"Completato! Totale: {totalKm:F2} km";
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusMessage = "Annullamento in corso...";
    }

    [RelayCommand]
    private void SaveResults()
    {
        if (FilePath is null || !HasResults) return;

        var config = BuildConfig();
        var error = _excelService.SaveResults(FilePath, [.. Rows], config, TotalDistanceKm);

        if (error is null)
        {
            SaveSuccess = true;
            StatusMessage = $"Risultati salvati in: {FileName}";
        }
        else
        {
            var dlg = new SaveFileDialog
            {
                Title = "Salva come nuovo file",
                Filter = "File Excel (*.xlsx)|*.xlsx",
                FileName = Path.GetFileNameWithoutExtension(FilePath) + "_distanze.xlsx"
            };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(FilePath, dlg.FileName, true);
                var newError = _excelService.SaveResults(dlg.FileName, [.. Rows], config, TotalDistanceKm);
                if (newError is null)
                {
                    SaveSuccess = true;
                    StatusMessage = $"Salvato in: {Path.GetFileName(dlg.FileName)}";
                }
                else
                {
                    StatusMessage = $"Errore salvataggio: {newError}";
                }
            }
        }
    }

    [RelayCommand]
    private async Task ValidateApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) return;

        IsValidatingKey = true;
        ApiKeyStatus = "Verifica in corso...";
        IsApiKeyValid = false;

        var (valid, message) = await _googleMaps.ValidateApiKeyAsync(ApiKey);
        IsApiKeyValid = valid;
        ApiKeyStatus = message;
        IsValidatingKey = false;

        if (valid) SaveSettings();
    }

    [RelayCommand]
    private void Clear()
    {
        FilePath = null;
        FileName = "Nessun file selezionato";
        _loadResult = null;
        Rows.Clear();
        ColumnOptions.Clear();
        OptionalColumnOptions.Clear();
        Progress = 0;
        TotalDistanceKm = 0;
        HasResults = false;
        SaveSuccess = false;
        StatusMessage = "Pronto";
        ProcessedInfo = "";
    }

    private ColumnConfig BuildConfig() => new()
    {
        DepartureAddressColumn = DepartureColumnIndex,
        ArrivalAddressColumn = ArrivalColumnIndex,
        DepartureDescriptionColumn = DepDescColumnIndex,
        ArrivalDescriptionColumn = ArrDescColumnIndex,
        HasHeaderRow = HasHeaderRow
    };

    private static string GetColumnLetter(int index)
    {
        string result = "";
        index++;
        while (index > 0)
        {
            index--;
            result = (char)('A' + index % 26) + result;
            index /= 26;
        }
        return result;
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new { ApiKey }));
        }
        catch { }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            if (doc.TryGetProperty("ApiKey", out var keyEl))
                ApiKey = keyEl.GetString() ?? "";
        }
        catch { }
    }
}
