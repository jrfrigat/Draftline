using System.Globalization;
using System.Text.Json.Serialization;

namespace Draftline.App.Model;
/// <summary>Monochrome table rules.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TableStyle>))]
public enum TableStyle
{
    Grid,
    Rows,
    Columns,
    Plain,
    Zebra
}

/// <summary>A sorted row retaining its authored identity.</summary>
public sealed record TableRow(int Index, IReadOnlyList<string> Cells);
/// <summary>Stable row order and state shared by the table drawing and SVG.</summary>
public static class TableState
{
    /// <summary>Rows sorted by an entirely numeric column, otherwise by text.</summary>
    public static IReadOnlyList<TableRow> Ordered(WireframeElement e)
    {
        var rows = e.Rows.Select((cells, index) => new TableRow(index, cells)).ToList();
        if (e.SortColumn < 0 || e.SortColumn >= e.Columns.Count)
            return rows;
        string Cell(TableRow row) => e.SortColumn < row.Cells.Count ? row.Cells[e.SortColumn] : "";
        var numeric = rows.All(row => double.TryParse(Cell(row), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n));
        if (numeric)
            return (e.SortDescending ? rows.OrderByDescending(row => double.Parse(Cell(row), CultureInfo.InvariantCulture)) : rows.OrderBy(row => double.Parse(Cell(row), CultureInfo.InvariantCulture))).ToList();
        return (e.SortDescending ? rows.OrderByDescending(Cell, StringComparer.OrdinalIgnoreCase) : rows.OrderBy(Cell, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Clicking the same column reverses its order.</summary>
    public static void Sort(WireframeElement e, int column)
    {
        if (column < 0 || column >= e.Columns.Count)
            return;
        e.SortDescending = e.SortColumn == column && !e.SortDescending;
        e.SortColumn = column;
    }

    /// <summary>Safe defaults and indices for old files and edited rows.</summary>
    public static void Normalize(WireframeElement e)
    {
        if (e.Kind == WireframeKind.DataGrid)
            for (var index = 0; index < e.Rows.Count; index++)
            {
                var row = e.Rows[index] ??= new();
                if (row.Count > e.Columns.Count)
                    row.RemoveRange(e.Columns.Count, row.Count - e.Columns.Count);
                while (row.Count < e.Columns.Count)
                    row.Add(string.Empty);
            }

        e.SortColumn = Math.Clamp(e.SortColumn, -1, e.Columns.Count - 1);
        e.SelectedRow = Math.Clamp(e.SelectedRow, -1, e.Rows.Count - 1);
        if (!Enum.IsDefined(e.TableStyle))
            e.TableStyle = TableStyle.Grid;
    }

    /// <summary>Header text with its authored sort indicator.</summary>
    public static string Header(WireframeElement e, int index) => e.Columns[index] + (e.SortColumn == index ? e.SortDescending ? " ▾" : " ▴" : "");
}
