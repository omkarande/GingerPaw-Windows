namespace GingerPaw.TextProcessing;

/// <summary>
/// Safety net for when the local LLM produces output that doesn't look like a genuine
/// reformat of the input — e.g. it echoes its own system-prompt rules back as if they were
/// content, observed in testing with the small 0.5B model used here. Pasting exactly what
/// was said matters more than getting restructuring right every time, so callers should
/// fall back to the original transcript whenever this returns false.
/// </summary>
public static class OutputValidation
{
    private static readonly string[] SuspiciousPhrases =
    {
        "should follow these rules",
        "preserve the speaker",
        "preserve speaker",
        "do not add facts",
        "do not add anything",
        "never answer questions",
        "reformat this",
        "reformat dictated speech",
    };

    public static bool LooksValid(string formatted, string original)
    {
        if (formatted.Length == 0)
        {
            return false;
        }

        // Restructuring into a list adds some punctuation/newlines, but genuine output is
        // never multiple times longer than what was actually said.
        if (formatted.Length > original.Length * 3 + 40)
        {
            return false;
        }

        foreach (var phrase in SuspiciousPhrases)
        {
            if (formatted.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
