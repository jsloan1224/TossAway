# GridLab — Throwaway POC Spec

## Purpose

Prove out a template-driven, formula-capable data grid in Blazor Server, modeled on a supply chain planning report. This is a throwaway project — optimize for clarity and learning, not production-readiness.

The goal is to validate that this paradigm (named-reference formulas, template-defined rows, dependency-tracked recalc, typed values with per-row formatting) works and feels right before committing to a full implementation in a real project.

## Non-Goals

- No database. All data in-memory, initialized on app start.
- No multiple periods / months. Single column of values.
- No template editor UI. The template is hardcoded in C#.
- No authentication.
- No persistence. Refreshing the page resets state.
- No unit tests required (but welcome if trivial).
- Minimal styling. Functional, not pretty.

## Tech Stack

- **.NET 9** (SDK already installed)
- **Blazor Server** (Interactive Server render mode, .NET 9 "Blazor Web App" template)
- **NCalc** — use the latest stable NuGet package (NCalc 5.x as of writing). Package ID: `NCalc`
- **No third-party grid libraries.** Build the grid from raw HTML table + Blazor. We want to feel the primitives.
- **No CSS framework.** Plain CSS in a scoped stylesheet or a single `site.css`. Keep it minimal.

## Domain Model

### Core Concepts

- **Template** — a definition of rows with formulas, format strings, and value types. Hardcoded for the POC.
- **Cell** — a single value instance. Holds either an input value (user-entered) or a computed value (from formula evaluation).
- **Formula Engine** — NCalc-based evaluator that resolves named row references.
- **Recalc Service** — determines evaluation order via dependency graph, recalculates dependent cells on edit.

### Types

```csharp
public enum RowType
{
    Header,   // non-data row, just a label (e.g. "Production Needs")
    Input,    // user enters value
    Calc,     // value comes from formula
    Total     // semantically a total; treat as Calc for POC
}

public enum ValueType
{
    Number,   // double, formatted with FormatString
    Text,     // string, no numeric formatting
    Percent   // double stored as decimal (0.05 = 5%), formatted with P-family strings
}

public class TemplateRow
{
    public required string RowKey { get; init; }     // unique id, snake_case, used in formulas
    public required string DisplayName { get; init; }
    public required int Order { get; init; }
    public required RowType RowType { get; init; }
    public ValueType ValueType { get; init; } = ValueType.Number;
    public string? DefaultFormula { get; init; }     // null for Input/Header rows
    public string FormatString { get; init; } = "N0";
    public int IndentLevel { get; init; } = 0;       // for visual hierarchy under headers
}

public class CellState
{
    public required string RowKey { get; set; }
    public object? Value { get; set; }               // double, string, or null
    public string? FormulaOverride { get; set; }     // null = use template default
    public string? Error { get; set; }               // set when formula eval fails
}
```

### The POC Template (hardcoded)

Create a static `TemplateRegistry` class exposing one template called `SupplyPlanMonth` with these rows, in order:

| Order | RowKey              | DisplayName         | RowType | ValueType | Default Formula                                                | Format | Indent |
|-------|---------------------|---------------------|---------|-----------|----------------------------------------------------------------|--------|--------|
| 10    | product_code        | Product Code        | Input   | Text      | —                                                              | —      | 0      |
| 20    | supply              | Supply              | Input   | Number    | —                                                              | N0     | 0      |
| 30    | shrink              | Shrink              | Input   | Number    | —                                                              | N0     | 0      |
| 40    | net_supply          | Net Supply          | Calc    | Number    | `supply - shrink`                                              | N0     | 0      |
| 50    | production_header   | Production Needs    | Header  | Text      | —                                                              | —      | 0      |
| 60    | portioning          | Portioning          | Input   | Number    | —                                                              | N0     | 1      |
| 70    | line_run            | Line Run            | Input   | Number    | —                                                              | N0     | 1      |
| 80    | rocketman           | Rocketman           | Input   | Number    | —                                                              | N0     | 1      |
| 90    | nuggets             | Nuggets             | Input   | Number    | —                                                              | N0     | 1      |
| 100   | total_production    | Total Production    | Total   | Number    | `portioning + line_run + rocketman + nuggets`                  | N0     | 1      |
| 110   | net_available       | Net Available       | Calc    | Number    | `net_supply - total_production`                                | N0     | 0      |
| 120   | shrink_rate         | Shrink Rate         | Calc    | Percent   | `shrink / supply`                                              | P2     | 0      |

Seed these initial input values:
- product_code = "12345"
- supply = 10000
- shrink = 200
- portioning = 5000
- line_run = 1000
- rocketman = 1750
- nuggets = 1500

With these inputs the calc rows should compute:
- net_supply = 9800
- total_production = 9250
- net_available = 550
- shrink_rate = 0.02 (displayed as 2.00%)

## Architecture

```
/Services
    FormulaEngine.cs        — NCalc wrapper, evaluates one expression given a dict of named values
    DependencyGraph.cs      — parses formulas to extract identifiers, builds DAG, provides topological order
    SheetState.cs           — holds current cell values + overrides, owns recalc orchestration
/Models
    TemplateRow.cs
    CellState.cs
    Enums.cs
/Templates
    TemplateRegistry.cs     — hardcoded templates
/Components/Pages
    Sheet.razor             — the main page: formula bar + grid
/Components/Grid
    SheetGrid.razor         — renders the rows
    SheetCell.razor         — one cell, handles display/edit mode
    FormulaBar.razor        — top-of-page formula input for selected cell
```

## Formula Engine

Wrap NCalc 5.x. One public method:

```csharp
public class FormulaEngine
{
    public EvalResult Evaluate(string expression, IReadOnlyDictionary<string, object?> namedValues);
}

public record EvalResult(bool Success, object? Value, string? Error);
```

Inside, create an NCalc `Expression`, hook its parameter-resolution event (or use the `Parameters` dictionary — NCalc 5.x API), resolve each identifier from `namedValues`, return the computed result. Catch exceptions and return them as `Error`.

Notes:
- Identifiers are row keys (e.g. `supply`, `net_supply`). Case-sensitive.
- If a referenced identifier is missing from `namedValues`, return an error — do NOT silently treat as zero.
- If a referenced value is null, return an error.
- If a referenced value is a string (Text-typed row), return an error — arithmetic on text is invalid.

## Dependency Graph

```csharp
public class DependencyGraph
{
    public DependencyGraph(IEnumerable<TemplateRow> rows, Func<string, string?> formulaResolver);
    public IReadOnlyList<string> EvaluationOrder { get; }   // topological sort of calc rows only
    public IReadOnlyList<string> GetDependents(string rowKey);  // forward dependents
    public bool HasCycle { get; }
}
```

Implementation:
- For each Calc/Total row, parse its formula (use NCalc to parse and walk the AST for identifiers, OR a simple regex `\b[a-z_][a-z0-9_]*\b` — regex is fine for POC).
- Build adjacency list: `rowKey -> rows it depends on`.
- Topological sort (Kahn's algorithm). Throw or flag on cycle.
- Expose forward-edge lookup so `SheetState` can find downstream cells when an input changes.

## SheetState

The central mutable state holder. Scoped service, injected into Sheet.razor.

```csharp
public class SheetState
{
    public IReadOnlyList<TemplateRow> Rows { get; }
    public IReadOnlyDictionary<string, CellState> Cells { get; }
    public event Action? StateChanged;

    public void SetInputValue(string rowKey, object? value);
    public void SetFormulaOverride(string rowKey, string? formula);  // null = revert to template
    public string? GetEffectiveFormula(string rowKey);
    public void RecalculateAll();
}
```

Behavior:
- On construction, load the template, seed initial inputs, run `RecalculateAll()`.
- On `SetInputValue`, update the cell, walk dependents in topo order, recompute them.
- On `SetFormulaOverride`, rebuild the dependency graph (formulas changed → edges changed), then `RecalculateAll()`.
- Raise `StateChanged` after any mutation so Blazor re-renders.

## UI Components

### Sheet.razor (page, routed at `/`)

Layout top-to-bottom:
1. **Formula bar** at top — shows the effective formula of the currently selected cell. Editable. Enter commits.
2. **Grid** below — renders rows. Click selects. Double-click (or Enter/F2) enters edit mode on the cell.

Selected cell is tracked in page state (`string? _selectedRowKey`).

### SheetGrid.razor

Renders an HTML `<table>`. One row per `TemplateRow`. Columns:
- Display name (with indent via `padding-left: {IndentLevel}em`)
- Value cell (renders `SheetCell`)

Rows of `RowType.Header` render as a single merged cell (colspan=2) with header styling. Skip the value cell for headers.

### SheetCell.razor

Parameters: `TemplateRow Row`, `CellState Cell`, `bool IsSelected`, `bool IsEditing`, callbacks for select/commit.

Display mode:
- If `Cell.Error != null`, render the error text in red.
- If `Row.RowType == Calc || Total`, render the formatted computed value (read-only).
- If `Row.RowType == Input`, render the formatted value, clickable to edit.
- Text rows: raw string, no formatting.
- Number/Percent rows: `value.ToString(Row.FormatString)`.

Edit mode (Input rows only — Calc/Total rows are not directly editable in the grid; their formulas are edited via the formula bar):
- `<input>` element bound to a local buffer (raw string)
- On Enter or blur: parse per `ValueType`, commit to state
- On Escape: cancel

Visual distinction (via CSS classes):
- Input cells: white background
- Calc/Total cells: light gray background
- Overridden formula cells: small indicator (e.g. italic, or a dot)
- Error cells: red text

### FormulaBar.razor

Parameter: selected row key + current effective formula + callback for commit.

Behavior:
- Shows `=<formula>` when the selected row is Calc/Total, or the raw value for Input rows.
- Editing a Calc/Total formula and pressing Enter calls `SheetState.SetFormulaOverride`.
- Editing an Input value commits the value (same as inline editing).
- Shows row key and display name as a label next to the input for context.

## Implementation Order for Claude Code

Do these in strict sequence. Do not jump ahead. Verify each compiles and runs before moving on.

1. **Scaffold** — `dotnet new blazor -n GridLab --interactivity Server` (or the .NET 9 equivalent) in the repo root. Confirm it runs.
2. **Add NCalc** — `dotnet add package NCalc` (latest stable).
3. **Models + Enums** — create the POCOs under `/Models`. No logic yet.
4. **TemplateRegistry** — hardcoded `SupplyPlanMonth` template with the rows and seed values above.
5. **FormulaEngine** — implement `Evaluate`. Write a tiny console `Main` test or a Razor test page proving it computes `supply - shrink` given a dict.
6. **DependencyGraph** — implement with regex identifier extraction. Topological sort. Test with the POC template.
7. **SheetState** — implement. Wire to DI as a **Scoped** service (one per user circuit). On construction, build graph + initial recalc.
8. **SheetCell, SheetGrid** — render the table in display mode only. No editing yet. Verify values render correctly with formatting.
9. **Input editing** — add edit mode to `SheetCell` for Input rows. Commit to `SheetState.SetInputValue`. Verify recalc works.
10. **Formula bar** — add `FormulaBar.razor`. Wire selection. Allow editing formulas on Calc/Total rows. Verify overrides work and persist across other edits.
11. **Polish** — error display, selected-cell highlight, light CSS for the cell type colors.

After step 11: stop. Demo to JD.

## Guardrails

- Do not add a database, EF Core, or persistence.
- Do not add authentication or authorization.
- Do not add a CSS framework (Bootstrap, Tailwind, MudBlazor, etc.).
- Do not add a grid library.
- Do not add unit test projects unless explicitly asked.
- Do not refactor into "clean architecture" layers with separate projects. Single project.
- Do not try to generalize to multiple periods or templates. Hardcoded single template is intentional.
- If you want to deviate from this spec, stop and ask JD first.

## Success Criteria

- App launches with `dotnet run`, serves at `https://localhost:xxxx`.
- Grid renders all 12 rows with correct formatting. Calculated rows show correct computed values on initial load.
- Clicking an Input cell lets me edit the value. Pressing Enter commits and downstream calcs update.
- Clicking a Calc cell selects it and shows its formula in the formula bar. Editing the formula and pressing Enter applies the override. Downstream calcs re-evaluate correctly.
- Changing `supply` to 20000 updates `net_supply` to 19800, `net_available` to 10550, `shrink_rate` to 1.00%.
- Product code row accepts text (e.g. "ABC-123") without errors.
- If I edit `net_supply`'s formula to `supply * 2 - shrink`, the value becomes 19800 (with supply=10000, shrink=200). Reverting to the default via a blank formula (or however JD specifies later) restores the original behavior.
- A formula referencing a nonexistent row (e.g. `supply - xyz`) shows an error in that cell, does not crash the page.
