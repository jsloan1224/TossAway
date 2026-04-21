using GridLab.Models;
using GridLab.Templates;

namespace GridLab.Services;

public class SheetState
{
    private readonly FormulaEngine _engine = new();
    private readonly List<TemplateRow> _rows;
    private readonly Dictionary<string, CellState> _cells = new();
    private DependencyGraph _graph;

    public IReadOnlyList<TemplateRow> Rows => _rows;
    public IReadOnlyDictionary<string, CellState> Cells => _cells;
    public event Action? StateChanged;

    public SheetState()
    {
        _rows = [.. TemplateRegistry.SupplyPlanMonth.OrderBy(r => r.Order)];

        foreach (var row in _rows)
            _cells[row.RowKey] = new CellState { RowKey = row.RowKey };

        foreach (var (key, val) in TemplateRegistry.SupplyPlanMonthSeedValues)
            _cells[key].Value = val;

        _graph = BuildGraph();
        RecalculateAll();
    }

    public void SetInputValue(string rowKey, object? value)
    {
        _cells[rowKey].Value = value;
        _cells[rowKey].Error = null;
        RecalculateDependents(rowKey);
        StateChanged?.Invoke();
    }

    public void SetFormulaOverride(string rowKey, string? formula)
    {
        var row = _rows.First(r => r.RowKey == rowKey);
        if (row.RowType is not (RowType.Calc or RowType.Total))
            throw new InvalidOperationException($"Cannot set formula override on row '{rowKey}' of type {row.RowType}.");

        _cells[rowKey].FormulaOverride = string.IsNullOrWhiteSpace(formula) ? null : formula;
        _graph = BuildGraph();
        RecalculateAll();
        StateChanged?.Invoke();
    }

    public string? GetEffectiveFormula(string rowKey)
    {
        var cell = _cells[rowKey];
        if (cell.FormulaOverride is not null) return cell.FormulaOverride;
        return _rows.First(r => r.RowKey == rowKey).DefaultFormula;
    }

    public void RecalculateAll()
    {
        var namedValues = BuildNamedValues();
        foreach (var key in _graph.EvaluationOrder)
            EvaluateCell(key, namedValues);
    }

    private void RecalculateDependents(string changedKey)
    {
        var namedValues = BuildNamedValues();
        var visited = new HashSet<string>();
        var queue = new Queue<string>(_graph.GetDependents(changedKey));

        while (queue.Count > 0)
        {
            var key = queue.Dequeue();
            if (!visited.Add(key)) continue;

            EvaluateCell(key, namedValues);

            foreach (var dep in _graph.GetDependents(key))
                queue.Enqueue(dep);
        }
    }

    private void EvaluateCell(string rowKey, Dictionary<string, object?> namedValues)
    {
        var formula = GetEffectiveFormula(rowKey);
        if (formula is null)
        {
            _cells[rowKey].Error = "No formula.";
            return;
        }

        var result = _engine.Evaluate(formula, namedValues);
        _cells[rowKey].Value = result.Success ? result.Value : null;
        _cells[rowKey].Error = result.Success ? null : result.Error;

        // Update named values so downstream cells see the new value
        namedValues[rowKey] = result.Success ? result.Value : null;
    }

    private Dictionary<string, object?> BuildNamedValues() =>
        _cells.ToDictionary(kv => kv.Key, kv => kv.Value.Value);

    private DependencyGraph BuildGraph() =>
        new(_rows, GetEffectiveFormula);
}
