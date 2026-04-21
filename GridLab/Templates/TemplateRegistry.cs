using GridLab.Models;
using ValueType = GridLab.Models.ValueType;

namespace GridLab.Templates;

public static class TemplateRegistry
{
    public static IReadOnlyList<TemplateRow> SupplyPlanMonth { get; } =
    [
        new TemplateRow { RowKey = "product_code",      DisplayName = "Product Code",      Order = 10,  RowType = RowType.Input,  ValueType = ValueType.Text,    FormatString = "",   IndentLevel = 0 },
        new TemplateRow { RowKey = "supply",            DisplayName = "Supply",            Order = 20,  RowType = RowType.Input,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 0 },
        new TemplateRow { RowKey = "shrink",            DisplayName = "Shrink",            Order = 30,  RowType = RowType.Input,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 0 },
        new TemplateRow { RowKey = "net_supply",        DisplayName = "Net Supply",        Order = 40,  RowType = RowType.Calc,   ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 0, DefaultFormula = "supply - shrink" },
        new TemplateRow { RowKey = "production_header", DisplayName = "Production Needs",  Order = 50,  RowType = RowType.Header, ValueType = ValueType.Text,    FormatString = "",   IndentLevel = 0 },
        new TemplateRow { RowKey = "portioning",        DisplayName = "Portioning",        Order = 60,  RowType = RowType.Input,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 1 },
        new TemplateRow { RowKey = "line_run",          DisplayName = "Line Run",          Order = 70,  RowType = RowType.Input,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 1 },
        new TemplateRow { RowKey = "rocketman",         DisplayName = "Rocketman",         Order = 80,  RowType = RowType.Input,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 1 },
        new TemplateRow { RowKey = "nuggets",           DisplayName = "Nuggets",           Order = 90,  RowType = RowType.Input,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 1 },
        new TemplateRow { RowKey = "total_production",  DisplayName = "Total Production",  Order = 100, RowType = RowType.Total,  ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 1, DefaultFormula = "portioning + line_run + rocketman + nuggets" },
        new TemplateRow { RowKey = "net_available",     DisplayName = "Net Available",     Order = 110, RowType = RowType.Calc,   ValueType = ValueType.Number,  FormatString = "N0", IndentLevel = 0, DefaultFormula = "net_supply - total_production" },
        new TemplateRow { RowKey = "shrink_rate",       DisplayName = "Shrink Rate",       Order = 120, RowType = RowType.Calc,   ValueType = ValueType.Percent, FormatString = "P2", IndentLevel = 0, DefaultFormula = "shrink / supply" },
    ];

    public static IReadOnlyDictionary<string, object?> SupplyPlanMonthSeedValues { get; } =
        new Dictionary<string, object?>
        {
            ["product_code"] = "12345",
            ["supply"]       = 10000.0,
            ["shrink"]       = 200.0,
            ["portioning"]   = 5000.0,
            ["line_run"]     = 1000.0,
            ["rocketman"]    = 1750.0,
            ["nuggets"]      = 1500.0,
        };
}
