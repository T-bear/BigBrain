using System.Text;

namespace BigBrain.Api.Finance;

// Interprets one physical CSV line only. Reading, headers and domain validation
// remain with the intake store; this preserves its existing quote semantics.
internal static class FinanceDatasetCsvTokenizer
{
    internal static List<string> Tokenize(string line)
    {
        var fields = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                fields.Add(value.ToString());
                value.Clear();
            }
            else value.Append(ch);
        }
        if (quoted) throw new InvalidDataException("Unterminated quoted CSV field.");
        fields.Add(value.ToString());
        return fields;
    }
}
