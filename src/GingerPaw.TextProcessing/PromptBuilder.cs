namespace GingerPaw.TextProcessing;

/// <summary>
/// Builds the ChatML-formatted prompt sent to the local LLM to reformat a raw transcript.
/// Originally a verbatim port of MLXTextProcessor.swift's prompt(for:) — one big block of
/// rules + 6 long examples fed as plain completion text — but that consistently broke down
/// in testing with the 0.5B Qwen model used here: it would echo the rules back as content,
/// copy few-shot example text verbatim, or fall back to generic "I am an AI assistant..."
/// boilerplate. Root cause: Qwen2.5-Instruct is trained on the ChatML turn format
/// (`&lt;|im_start|&gt;`/`&lt;|im_end|&gt;`), and feeding it a plain-text completion prompt instead of
/// that format leaves a small instruct model with no reliable signal that it's mid-task at
/// all. This rebuilds the same 4 short example patterns (numbered list, bulleted list, plain
/// passthrough, question passthrough) as real user/assistant turns instead, which is the
/// reliable way to few-shot prompt a chat-tuned model. Kept as a pure function (no model
/// dependency) so it's unit-testable without loading anything.
/// </summary>
public static class PromptBuilder
{
    private const string SystemMessage =
        "You reformat dictated speech into clean text. Keep the speaker's exact meaning " +
        "and facts — never add anything new. If the speech describes an ordered sequence " +
        "of steps or tasks, format it as a numbered list (1. 2. 3.). If it's an unordered " +
        "set of items, use a bulleted list (\"- \"). Otherwise, output clean prose with " +
        "filler words (um, uh, like) removed. Never answer questions, only reformat them. " +
        "Output only the reformatted text, nothing else — no labels, no explanations.";

    private static readonly (string User, string Assistant)[] Examples =
    {
        ("first call the dentist then finish the report and finally pick up groceries",
         "1. Call the dentist\n2. Finish the report\n3. Pick up groceries"),
        // Covers the "the Nth thing is X" phrasing pattern specifically — testing showed
        // the model caught "first... then... finally" but missed this equally common
        // ordered-list phrasing without a second example to generalize from.
        ("first thing is call mom, second thing is buy milk, third thing is walk the dog",
         "1. Call mom\n2. Buy milk\n3. Walk the dog"),
        ("i need to fix the bug update the docs and ping marketing",
         "- Fix the bug\n- Update the docs\n- Ping marketing"),
        ("remind me to send the invoice tomorrow morning",
         "Remind me to send the invoice tomorrow morning."),
        ("what time is the meeting tomorrow",
         "What time is the meeting tomorrow?"),
    };

    public static string Build(string transcript)
    {
        var prompt = new System.Text.StringBuilder();
        prompt.Append("<|im_start|>system\n").Append(SystemMessage).Append("<|im_end|>\n");
        foreach (var (user, assistant) in Examples)
        {
            prompt.Append("<|im_start|>user\n").Append(user).Append("<|im_end|>\n");
            prompt.Append("<|im_start|>assistant\n").Append(assistant).Append("<|im_end|>\n");
        }
        prompt.Append("<|im_start|>user\n").Append(transcript).Append("<|im_end|>\n");
        prompt.Append("<|im_start|>assistant\n");
        return prompt.ToString();
    }
}
