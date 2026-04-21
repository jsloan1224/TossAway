namespace GridLab.Models;

public class TemplateRow
{
    public required string RowKey { get; init; }
    public required string DisplayName { get; init; }
    public required int Order { get; init; }
    public required RowType RowType { get; init; }
    public ValueType ValueType { get; init; } = ValueType.Number;
    public string? DefaultFormula { get; init; }
    public string FormatString { get; init; } = "N0";
    public int IndentLevel { get; init; } = 0;
}
