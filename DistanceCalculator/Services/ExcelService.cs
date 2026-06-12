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
    private static readonly string[] ItalianAddressKeywords =
        ["via ", "viale ", "piazza ", "corso ", "vicolo ", "largo ", "strada ", "loc.", "localit"];

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

    public List<AddressRow> BuildRows(ExcelLoadResult loadResult, ColumnConfig config)
    {
        var rows = new List<AddressRow>();
        int startIdx = config.HasHeaderRow ? 1 : 0;
        int rowNum = config.HasHeaderRow ? 2 : 1;

        for (int i = startIdx; i < loadResult.RawData.Count; i++)
        {
            var data = loadResult.RawData[i];
            string GetCell(int idx) => idx >= 0 && idx < data.Length ? data[idx].Trim() : "";

            string depRaw = GetCell(config.DepartureAddressColumn);
            string arrRaw = GetCell(config.ArrivalAddressColumn);

            if (string.IsNullOrWhiteSpace(depRaw) && string.IsNullOrWhiteSpace(arrRaw))
            {
                rowNum++;
                continue;
            }

            string depDesc, depAddr, arrDesc, arrAddr;

            if (config.DepartureCombinedCell)
                (depDesc, depAddr) = SplitDescriptionAndAddress(depRaw, config.CellSeparator);
            else
            {
                depDesc = GetCell(config.DepartureDescriptionColumn);
                depAddr = depRaw;
            }

            if (config.ArrivalCombinedCell)
                (arrDesc, arrAddr) = SplitDescriptionAndAddress(arrRaw, config.CellSeparator);
            else
            {
                arrDesc = GetCell(config.ArrivalDescriptionColumn);
                arrAddr = arrRaw;
            }

            rows.Add(new AddressRow
            {
                RowNumber = rowNum,
                Date = GetCell(config.DateColumn),
                GenericDescription = GetCell(config.GenericDescriptionColumn),
                DepartureRaw = depRaw,
                DepartureDescription = depDesc,
                DepartureAddress = depAddr,
                ArrivalRaw = arrRaw,
                ArrivalDescription = arrDesc,
                ArrivalAddress = arrAddr,
                Status = string.IsNullOrWhiteSpace(depAddr) || string.IsNullOrWhiteSpace(arrAddr)
                    ? ProcessingStatus.Skipped
                    : ProcessingStatus.Pending
            });
            rowNum++;
        }

        return rows;
    }

    /// <summary>
    /// Splits a cell value that may contain "Site description\nStreet address, City".
    /// Strategy: newline → explicit separator → address keyword → whole cell as address.
    /// </summary>
    public static (string Description, string Address) SplitDescriptionAndAddress(
        string cellValue, string separator = "auto")
    {
        if (string.IsNullOrWhiteSpace(cellValue))
            return ("", "");

        // Normalize line endings
        string normalized = cellValue.Replace("\r\n", "\n").Replace('\r', '\n');

        // Explicit separator requested
        if (separator != "auto")
        {
            string sep = separator == "\\n" ? "\n" : separator;
            int sepIdx = normalized.IndexOf(sep, StringComparison.Ordinal);
            if (sepIdx > 0)
                return (normalized[..sepIdx].Trim(), normalized[(sepIdx + sep.Length)..].Trim());
        }

        // Auto mode: try newline first
        int nlIdx = normalized.IndexOf('\n');
        if (nlIdx > 0)
        {
            string firstLine = normalized[..nlIdx].Trim();
            string rest = normalized[(nlIdx + 1)..].Replace('\n', ' ').Trim();
            if (!string.IsNullOrWhiteSpace(rest))
                return (firstLine, rest);
        }

        // Try " - " (description - address)
        int dashIdx = normalized.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0)
            return (normalized[..dashIdx].Trim(), normalized[(dashIdx + 3)..].Trim());

        // Try address keyword detection for Italian addresses
        string lower = normalized.ToLowerInvariant();
        foreach (var keyword in ItalianAddressKeywords)
        {
            int kidx = lower.IndexOf(keyword, StringComparison.Ordinal);
            if (kidx > 0)
            {
                string desc = normalized[..kidx].Trim().TrimEnd(',', ';', '-', ' ');
                string addr = normalized[kidx..].Trim();
                return (desc, addr);
            }
        }

        // No separator found: treat the whole cell as the address
        return ("", normalized.Trim());
    }

    public string? SaveResults(string filePath, List<AddressRow> rows, ColumnConfig config, double totalKm)
    {
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First();

            int distanceCol = config.DistanceColumnIndex + 1;
            int dataStart = config.HasHeaderRow ? 2 : 1;

            if (config.HasHeaderRow)
            {
                var hdr = ws.Cell(1, distanceCol);
                hdr.Value = "Distanza (km)";
                hdr.Style.Font.Bold = true;
                hdr.Style.Font.FontColor = XLColor.White;
                hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                hdr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
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
            var lblCell = ws.Cell(totalRow, labelCol);
            lblCell.Value = "TOTALE KM:";
            lblCell.Style.Font.Bold = true;
            lblCell.Style.Font.FontSize = 12;
            lblCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var totCell = ws.Cell(totalRow, distanceCol);
            totCell.Value = totalKm;
            totCell.Style.NumberFormat.Format = "#,##0.00";
            totCell.Style.Font.Bold = true;
            totCell.Style.Font.FontSize = 12;
            totCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#BBDEFB");
            totCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.Column(distanceCol).AdjustToContents();

            workbook.Save();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public (int DateCol, int GenericDescCol, int DepartureCol, int ArrivalCol,
            int DepDescCol, int ArrDescCol) AutoDetectColumns(List<string> headers)
    {
        int date = -1, generic = -1, dep = -1, arr = -1, depDesc = -1, arrDesc = -1;

        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].ToLowerInvariant().Trim();
            if (date < 0 && (h == "data" || h.Contains("data") || h.Contains("date")))
                date = i;
            else if (generic < 0 && (h.Contains("descrizione") || h.Contains("descr") || h.Contains("note")))
                generic = i;
            else if (dep < 0 && (h.Contains("partenza") || h.Contains("origine") || h.Contains("from") || h == "da"))
                dep = i;
            else if (arr < 0 && (h.Contains("arrivo") || h.Contains("destinazione") || h.Contains("to") || h == "a"))
                arr = i;
            else if (depDesc < 0 && h.Contains("desc") && (h.Contains("partenza") || h.Contains("origine")))
                depDesc = i;
            else if (arrDesc < 0 && h.Contains("desc") && (h.Contains("arrivo") || h.Contains("dest")))
                arrDesc = i;
        }

        return (date >= 0 ? date : 0,
                generic >= 0 ? generic : 1,
                dep >= 0 ? dep : 2,
                arr >= 0 ? arr : 3,
                depDesc, arrDesc);
    }

    private static bool HasMeaningfulHeader(List<string> headers) =>
        headers.Any(h =>
        {
            var l = h.ToLowerInvariant();
            return l.Contains("partenza") || l.Contains("arrivo") || l.Contains("indirizzo") ||
                   l.Contains("address") || l.Contains("from") || l.Contains("to") ||
                   l == "data" || l.Contains("descrizione");
        });
}
