using ClosedXML.Excel;
using DistanceCalculator.Models;

namespace DistanceCalculator.Services;

public class ExcelLoadResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Headers { get; set; } = [];
    public List<string[]> RawData { get; set; } = [];
    public int ColumnCount { get; set; }
    public bool HasHeaders { get; set; }
}

public class ExcelService
{
    public ExcelLoadResult LoadFile(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastRow == 0 || lastCol == 0)
                return new ExcelLoadResult { Success = false, Error = "Il file è vuoto" };

            var headers = new List<string>();
            for (int col = 1; col <= lastCol; col++)
                headers.Add(ws.Cell(1, col).GetString());

            // Load all rows including row 1; BuildRows will skip row 1 if HasHeaderRow=true
            var rawData = new List<string[]>();
            for (int row = 1; row <= lastRow; row++)
            {
                var rowData = new string[lastCol];
                for (int col = 1; col <= lastCol; col++)
                    rowData[col - 1] = ws.Cell(row, col).GetString();
                rawData.Add(rowData);
            }

            return new ExcelLoadResult
            {
                Success = true,
                Headers = headers,
                RawData = rawData,
                ColumnCount = lastCol,
                HasHeaders = HasMeaningfulHeader(headers)
            };
        }
        catch (Exception ex)
        {
            return new ExcelLoadResult { Success = false, Error = ex.Message };
        }
    }

    public string? SaveResults(string filePath, List<AddressRow> rows, ColumnConfig config, double totalKm)
    {
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First();

            int distanceCol = config.DistanceColumnIndex + 1; // 1-based
            int dataStart = config.HasHeaderRow ? 2 : 1;

            if (config.HasHeaderRow)
            {
                var headerCell = ws.Cell(1, distanceCol);
                headerCell.Value = "Distanza (km)";
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                int excelRow = dataStart + i;
                var row = rows[i];
                var cell = ws.Cell(excelRow, distanceCol);

                if (row.DistanceKm.HasValue)
                {
                    cell.Value = row.DistanceKm.Value;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
                }
                else if (row.Status == ProcessingStatus.Error)
                {
                    cell.Value = row.ErrorMessage ?? "Errore";
                    cell.Style.Font.FontColor = XLColor.Red;
                }
                else if (row.Status == ProcessingStatus.Skipped)
                {
                    cell.Value = "Riga saltata";
                    cell.Style.Font.FontColor = XLColor.Gray;
                }
            }

            int lastDataRow = dataStart + rows.Count - 1;
            int totalRow = lastDataRow + 2;

            int labelCol = Math.Max(1, distanceCol - 1);
            var labelCell = ws.Cell(totalRow, labelCol);
            labelCell.Value = "TOTALE KM:";
            labelCell.Style.Font.Bold = true;
            labelCell.Style.Font.FontSize = 12;
            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var totalCell = ws.Cell(totalRow, distanceCol);
            totalCell.Value = totalKm;
            totalCell.Style.NumberFormat.Format = "#,##0.00";
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Font.FontSize = 12;
            totalCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#BBDEFB");
            totalCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.Column(distanceCol).AdjustToContents();

            workbook.Save();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public List<AddressRow> BuildRows(ExcelLoadResult loadResult, ColumnConfig config)
    {
        var rows = new List<AddressRow>();
        int startIdx = config.HasHeaderRow ? 1 : 0; // skip index 0 (row 1) if it's a header
        int rowNum = config.HasHeaderRow ? 2 : 1;

        for (int i = startIdx; i < loadResult.RawData.Count; i++)
        {
            var data = loadResult.RawData[i];
            string GetCell(int idx) => idx >= 0 && idx < data.Length ? data[idx].Trim() : "";

            var departure = GetCell(config.DepartureAddressColumn);
            var arrival = GetCell(config.ArrivalAddressColumn);

            if (string.IsNullOrWhiteSpace(departure) && string.IsNullOrWhiteSpace(arrival))
            {
                rowNum++;
                continue;
            }

            rows.Add(new AddressRow
            {
                RowNumber = rowNum,
                DepartureAddress = departure,
                ArrivalAddress = arrival,
                DepartureDescription = GetCell(config.DepartureDescriptionColumn),
                ArrivalDescription = GetCell(config.ArrivalDescriptionColumn),
                Status = string.IsNullOrWhiteSpace(departure) || string.IsNullOrWhiteSpace(arrival)
                    ? ProcessingStatus.Skipped
                    : ProcessingStatus.Pending
            });
            rowNum++;
        }

        return rows;
    }

    public (int DepartureCol, int ArrivalCol, int DepDescCol, int ArrDescCol) AutoDetectColumns(List<string> headers)
    {
        int dep = -1, arr = -1, depDesc = -1, arrDesc = -1;

        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].ToLowerInvariant();
            if (dep < 0 && (h.Contains("partenza") || h.Contains("origine") || h.Contains("from") || h == "da"))
                dep = i;
            else if (arr < 0 && (h.Contains("arrivo") || h.Contains("destinazione") || h.Contains("to") || h == "a"))
                arr = i;
            else if (depDesc < 0 && h.Contains("desc") && (h.Contains("partenza") || h.Contains("origine")))
                depDesc = i;
            else if (arrDesc < 0 && h.Contains("desc") && (h.Contains("arrivo") || h.Contains("dest")))
                arrDesc = i;
        }

        return (dep >= 0 ? dep : 0, arr >= 0 ? arr : 1, depDesc, arrDesc);
    }

    private static bool HasMeaningfulHeader(List<string> headers) =>
        headers.Any(h => !string.IsNullOrWhiteSpace(h) &&
            (h.ToLowerInvariant().Contains("partenza") ||
             h.ToLowerInvariant().Contains("arrivo") ||
             h.ToLowerInvariant().Contains("indirizzo") ||
             h.ToLowerInvariant().Contains("address") ||
             h.ToLowerInvariant().Contains("from") ||
             h.ToLowerInvariant().Contains("to")));
}
