using System.Text.RegularExpressions;
using GridLab.Models;

namespace GridLab.Services;

public class DependencyGraph
{
    private static readonly Regex IdentifierRegex = new(@"\b[a-z_][a-z0-9_]*\b", RegexOptions.Compiled);

    private readonly IReadOnlyList<string> _evaluationOrder;
    private readonly Dictionary<string, List<string>> _dependents = new();

    public IReadOnlyList<string> EvaluationOrder => _evaluationOrder;
    public bool HasCycle { get; }

    public DependencyGraph(IEnumerable<TemplateRow> rows, Func<string, string?> formulaResolver)
    {
        var rowList = rows.ToList();
        var rowKeys = rowList.Select(r => r.RowKey).ToHashSet();
        var calcRows = rowList.Where(r => r.RowType is RowType.Calc or RowType.Total).ToList();

        // dependencies[key] = set of row keys this calc row depends on
        var dependencies = new Dictionary<string, HashSet<string>>();

        foreach (var row in calcRows)
        {
            _dependents[row.RowKey] = [];
            dependencies[row.RowKey] = [];

            var formula = formulaResolver(row.RowKey);
            if (formula is null) continue;

            foreach (Match m in IdentifierRegex.Matches(formula))
            {
                var id = m.Value;
                if (rowKeys.Contains(id) && id != row.RowKey)
                    dependencies[row.RowKey].Add(id);
            }
        }

        // Build forward edges: dependency -> dependents
        foreach (var (key, deps) in dependencies)
        {
            foreach (var dep in deps)
            {
                if (!_dependents.ContainsKey(dep))
                    _dependents[dep] = [];
                _dependents[dep].Add(key);
            }
        }

        // Kahn's algorithm — topo sort over calc rows only
        // inDegree[key] = number of calc-row predecessors key still depends on
        var inDegree = calcRows.ToDictionary(r => r.RowKey, _ => 0);
        foreach (var (key, deps) in dependencies)
        {
            foreach (var dep in deps)
            {
                if (inDegree.ContainsKey(dep)) // dep is itself a calc row
                    inDegree[key]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<string>();

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            sorted.Add(node);

            foreach (var dependent in _dependents.GetValueOrDefault(node, []))
            {
                if (!inDegree.ContainsKey(dependent)) continue;
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    queue.Enqueue(dependent);
            }
        }

        HasCycle = sorted.Count != calcRows.Count;
        _evaluationOrder = sorted;
    }

    public IReadOnlyList<string> GetDependents(string rowKey) =>
        _dependents.TryGetValue(rowKey, out var list) ? list : [];
}
