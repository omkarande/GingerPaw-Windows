using System.Linq;

namespace GingerPaw.TextProcessing;

/// <summary>
/// Verbatim port of MLXTextProcessor.swift's clean(_:fallback:) — strips a markdown code
/// fence the model sometimes wraps output in, strips a leading echo of the prompt's own
/// "Output:" label, and falls back to the original transcript if nothing real is left.
/// Kept as a pure function (no model dependency) so it's unit-testable without loading anything.
/// </summary>
public static class OutputCleaning
{
    private static readonly string[] Prefixes = { "Output:", "Output", "Reformatted:" };

    public static string Clean(string output, string fallback)
    {
        var result = output.Trim();

        if (result.StartsWith("```"))
        {
            var lines = result.Split('\n').ToList();
            if (lines.Count > 0 && lines[0].StartsWith("```"))
            {
                lines.RemoveAt(0);
            }
            if (lines.Count > 0 && lines[^1].Trim() == "```")
            {
                lines.RemoveAt(lines.Count - 1);
            }
            result = string.Join("\n", lines).Trim();
        }

        foreach (var prefix in Prefixes)
        {
            if (result.StartsWith(prefix))
            {
                result = result[prefix.Length..].Trim();
            }
        }

        return result.Length == 0 ? fallback : result;
    }
}
