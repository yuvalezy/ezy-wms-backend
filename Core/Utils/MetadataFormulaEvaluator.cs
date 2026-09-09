using System.Globalization;
using System.Text.RegularExpressions;

namespace Core.Utils;

/// <summary>
/// Evaluates <c>Calculated.Formula</c> expressions for item metadata on the backend.
/// Mirrors the frontend evaluator (features/metadata/hooks/useCalculatedFields.ts) so both sides
/// agree: <c>{FieldId}</c> tokens are substituted, only <c>+ - * / ( )</c> and unary minus are supported,
/// and any missing dependency, invalid expression or division by zero yields <c>null</c> — never a throw.
/// Arithmetic is done in <see cref="decimal"/> so tiny volumes keep their precision.
/// </summary>
public static partial class MetadataFormulaEvaluator {
    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^[\d\s+\-*/.()]+$")]
    private static partial Regex SafeExpressionRegex();

    /// <summary>Field IDs referenced as <c>{FieldId}</c> in the formula (distinct, trimmed).</summary>
    public static IReadOnlyList<string> GetReferencedFields(string? formula) {
        if (string.IsNullOrWhiteSpace(formula)) return [];
        return TokenRegex().Matches(formula)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Evaluates the formula. Returns <c>null</c> when any referenced field is missing / null /
    /// non-numeric, when the expression contains anything but numbers and arithmetic, or on
    /// division by zero. The result is rounded to <paramref name="precision"/> decimals.
    /// </summary>
    public static decimal? Evaluate(string? formula, IReadOnlyDictionary<string, object?> values, int precision) {
        if (string.IsNullOrWhiteSpace(formula)) return null;

        var missing = false;
        var expression = TokenRegex().Replace(formula, m => {
            var id = m.Groups[1].Value.Trim();
            var key = values.Keys.FirstOrDefault(k => string.Equals(k, id, StringComparison.OrdinalIgnoreCase));
            if (key == null || !TryToDecimal(values[key], out var d)) {
                missing = true;
                return "0";
            }
            return "(" + d.ToString(CultureInfo.InvariantCulture) + ")";
        });

        if (missing || !SafeExpressionRegex().IsMatch(expression)) return null;

        try {
            var parser = new Parser(expression);
            var result = parser.ParseExpression();
            if (!parser.AtEnd) return null;
            return Math.Round(result, Math.Clamp(precision, 0, 28), MidpointRounding.AwayFromZero);
        }
        catch (Exception) {
            // DivideByZero, Overflow, malformed input — all mean "no value", same as the frontend.
            return null;
        }
    }

    private static bool TryToDecimal(object? value, out decimal result) {
        switch (value) {
            case null:
                result = 0;
                return false;
            case decimal d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case double dbl:
                result = (decimal)dbl;
                return true;
            case float f:
                result = (decimal)f;
                return true;
            case string s:
                return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>Minimal recursive-descent parser: expr := term (('+'|'-') term)*; term := unary (('*'|'/') unary)*; unary := '-' unary | primary; primary := number | '(' expr ')'.</summary>
    private sealed class Parser(string text) {
        private int pos;

        public bool AtEnd {
            get {
                SkipWhitespace();
                return pos >= text.Length;
            }
        }

        public decimal ParseExpression() {
            var value = ParseTerm();
            while (true) {
                SkipWhitespace();
                if (Peek() == '+') { pos++; value += ParseTerm(); }
                else if (Peek() == '-') { pos++; value -= ParseTerm(); }
                else return value;
            }
        }

        private decimal ParseTerm() {
            var value = ParseUnary();
            while (true) {
                SkipWhitespace();
                if (Peek() == '*') { pos++; value *= ParseUnary(); }
                else if (Peek() == '/') { pos++; value /= ParseUnary(); }
                else return value;
            }
        }

        private decimal ParseUnary() {
            SkipWhitespace();
            if (Peek() == '-') { pos++; return -ParseUnary(); }
            if (Peek() == '+') { pos++; return ParseUnary(); }
            return ParsePrimary();
        }

        private decimal ParsePrimary() {
            SkipWhitespace();
            if (Peek() == '(') {
                pos++;
                var inner = ParseExpression();
                SkipWhitespace();
                if (Peek() != ')') throw new FormatException("Missing ')'");
                pos++;
                return inner;
            }

            var start = pos;
            while (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '.')) pos++;
            if (start == pos) throw new FormatException($"Unexpected character at {pos}");
            return decimal.Parse(text.AsSpan(start, pos - start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        private char Peek() => pos < text.Length ? text[pos] : '\0';

        private void SkipWhitespace() {
            while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
        }
    }
}
