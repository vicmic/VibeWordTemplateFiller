using CommunityToolkit.Mvvm.ComponentModel;

namespace DistanceCalculator.Models;

public partial class AddressRow : ObservableObject
{
    public int RowNumber { get; set; }

    [ObservableProperty]
    private string _date = "";

    [ObservableProperty]
    private string _genericDescription = "";

    [ObservableProperty]
    private string _departureRaw = "";

    [ObservableProperty]
    private string _departureDescription = "";

    [ObservableProperty]
    private string _departureAddress = "";

    [ObservableProperty]
    private string _arrivalRaw = "";

    [ObservableProperty]
    private string _arrivalDescription = "";

    [ObservableProperty]
    private string _arrivalAddress = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceDisplay))]
    private double? _distanceKm;

    [ObservableProperty]
    private ProcessingStatus _status = ProcessingStatus.Pending;

    [ObservableProperty]
    private string? _errorMessage;

    public string DistanceDisplay => DistanceKm.HasValue ? $"{DistanceKm.Value:F2} km" : "-";
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Success,
    Error,
    Skipped
}
