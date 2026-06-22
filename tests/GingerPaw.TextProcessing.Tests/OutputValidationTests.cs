using GingerPaw.TextProcessing;

namespace GingerPaw.TextProcessing.Tests;

public class OutputValidationTests
{
    [Fact]
    public void AcceptsAGenuineShortReformat()
    {
        var valid = OutputValidation.LooksValid(
            formatted: "1. Call the dentist\n2. Finish the report",
            original: "first call the dentist then finish the report");

        Assert.True(valid);
    }

    [Fact]
    public void RejectsEmptyOutput()
    {
        var valid = OutputValidation.LooksValid(formatted: "", original: "anything");

        Assert.False(valid);
    }

    [Fact]
    public void RejectsOutputThatLeakedTheSystemPromptRules()
    {
        var leaked = "My actual sentence.\n\nOutput should follow these rules:\n- Preserve the speaker's meaning";

        var valid = OutputValidation.LooksValid(formatted: leaked, original: "my actual sentence");

        Assert.False(valid);
    }

    [Fact]
    public void RejectsOutputThatIsImplausiblyLongerThanTheInput()
    {
        var original = "buy milk";
        var bloated = string.Concat(Enumerable.Repeat("buy milk and also do many other things. ", 10));

        var valid = OutputValidation.LooksValid(formatted: bloated, original: original);

        Assert.False(valid);
    }
}
