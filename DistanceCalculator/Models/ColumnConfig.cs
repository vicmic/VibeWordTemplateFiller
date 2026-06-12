namespace DistanceCalculator.Models;

public class ColumnConfig
{
    public int DepartureAddressColumn { get; set; } = 0;
    public int ArrivalAddressColumn { get; set; } = 1;
    public int DepartureDescriptionColumn { get; set; } = -1;
    public int ArrivalDescriptionColumn { get; set; } = -1;
    public bool HasHeaderRow { get; set; } = true;

    public int DistanceColumnIndex
    {
        get
        {
            int max = Math.Max(DepartureAddressColumn, ArrivalAddressColumn);
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
