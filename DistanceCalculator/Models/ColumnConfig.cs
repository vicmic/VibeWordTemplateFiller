namespace DistanceCalculator.Models;

public class ColumnConfig
{
    public int DateColumn { get; set; } = 0;
    public int GenericDescriptionColumn { get; set; } = 1;
    public int DepartureAddressColumn { get; set; } = 2;
    public int ArrivalAddressColumn { get; set; } = 3;
    public int DepartureDescriptionColumn { get; set; } = -1;
    public int ArrivalDescriptionColumn { get; set; } = -1;

    /// <summary>Departure cell may contain "Site Name\nStreet address" — split on separator.</summary>
    public bool DepartureCombinedCell { get; set; } = false;

    /// <summary>Arrival cell may contain "Site Name\nStreet address" — split on separator.</summary>
    public bool ArrivalCombinedCell { get; set; } = false;

    /// <summary>Separator used to split description from address inside a combined cell.</summary>
    public string CellSeparator { get; set; } = "auto";

    public bool HasHeaderRow { get; set; } = true;

    public int DistanceColumnIndex
    {
        get
        {
            int max = Math.Max(DepartureAddressColumn, ArrivalAddressColumn);
            if (DateColumn >= 0) max = Math.Max(max, DateColumn);
            if (GenericDescriptionColumn >= 0) max = Math.Max(max, GenericDescriptionColumn);
            if (DepartureDescriptionColumn >= 0) max = Math.Max(max, DepartureDescriptionColumn);
            if (ArrivalDescriptionColumn >= 0) max = Math.Max(max, ArrivalDescriptionColumn);
            return max + 1;
        }
    }
}

public class ColumnOption
{
    public int Index { get; set; }
    public string DisplayName { get; set; } = "";
}

public class SeparatorOption
{
    public string Value { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
