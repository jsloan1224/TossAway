using NCalc;

namespace GridLab.Services;

public record EvalResult(bool Success, object? Value, string? Error);

public class FormulaEngine
{
    public EvalResult Evaluate(string expression, IReadOnlyDictionary<string, object?> namedValues)
    {
        try
        {
            var expr = new Expression(expression);

            expr.EvaluateParameter += (name, args) =>
            {
                if (!namedValues.TryGetValue(name, out var val))
                    throw new InvalidOperationException($"Unknown identifier '{name}'.");


                if (val is null)
                    throw new InvalidOperationException($"Referenced value '{name}' is null.");

                if (val is string)
                    throw new InvalidOperationException($"Referenced value '{name}' is text and cannot be used in arithmetic.");

                args.Result = val;
            };

            var result = expr.Evaluate();

            return result switch
            {
                null  => new EvalResult(false, null, "Formula evaluated to null."),
                _     => new EvalResult(true, Convert.ToDouble(result), null)
            };
        }
        catch (Exception ex)
        {
            return new EvalResult(false, null, ex.Message);
        }
    }
}
