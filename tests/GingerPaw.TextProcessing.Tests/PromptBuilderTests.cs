using GingerPaw.TextProcessing;

namespace GingerPaw.TextProcessing.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void PutsTheTranscriptInAFinalUserTurn()
    {
        var prompt = PromptBuilder.Build("hello world");

        Assert.Contains("<|im_start|>user\nhello world<|im_end|>\n", prompt);
    }

    [Fact]
    public void EndsWithAnOpenAssistantTurnForTheRealInput()
    {
        var prompt = PromptBuilder.Build("anything");

        Assert.EndsWith("<|im_start|>user\nanything<|im_end|>\n<|im_start|>assistant\n", prompt);
    }

    [Fact]
    public void StartsWithASystemTurnContainingTheCoreRules()
    {
        var prompt = PromptBuilder.Build("x");

        Assert.StartsWith("<|im_start|>system\n", prompt);
        Assert.Contains("Never answer questions, only reformat them.", prompt);
        Assert.Contains("Output only the reformatted text, nothing else", prompt);
    }

    [Fact]
    public void ContainsAShortExampleTurnForEachPattern()
    {
        var prompt = PromptBuilder.Build("x");

        Assert.Contains("1. Call the dentist", prompt);
        Assert.Contains("1. Call mom\n2. Buy milk\n3. Walk the dog", prompt);
        Assert.Contains("- Fix the bug", prompt);
        Assert.Contains("Remind me to send the invoice tomorrow morning.", prompt);
        Assert.Contains("What time is the meeting tomorrow?", prompt);
    }

    [Fact]
    public void ExampleTurnsAreWrappedAsRealUserAndAssistantPairs()
    {
        var prompt = PromptBuilder.Build("x");

        Assert.Contains(
            "<|im_start|>user\nfirst call the dentist then finish the report and finally pick up groceries<|im_end|>\n" +
            "<|im_start|>assistant\n1. Call the dentist\n2. Finish the report\n3. Pick up groceries<|im_end|>\n",
            prompt);
    }

    [Fact]
    public void DoesNotEscapeQuotesInsideTheTranscript()
    {
        var prompt = PromptBuilder.Build("she said \"hi\"");

        Assert.Contains("she said \"hi\"<|im_end|>", prompt);
    }
}
