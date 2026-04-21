namespace GridLab.Models;

public class CellState
{
    public required string RowKey { get; set; }
    public object? Value { get; set; }
    public string? FormulaOverride { get; set; }
    public string? Error { get; set; }
}
