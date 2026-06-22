using GingerPaw.TextProcessing;

namespace GingerPaw.TextProcessing.Tests;

public class OutputCleaningTests
{
    [Fact]
    public void PassesAlreadyCleanTextThroughUnchanged()
    {
        var result = OutputCleaning.Clean("Today I'm going to do three things.", fallback: "fallback");

        Assert.Equal("Today I'm going to do three things.", result);
    }

    [Fact]
    public void StripsACodeFence()
    {
        var fenced = "```\nReformatted text here\n```";

        var result = OutputCleaning.Clean(fenced, fallback: "fallback");

        Assert.Equal("Reformatted text here", result);
    }

    [Theory]
    [InlineData("Output: Clean text")]
    [InlineData("OutputClean text")]
    [InlineData("Reformatted: Clean text")]
    public void StripsKnownLabelPrefixes(string labeled)
    {
        var result = OutputCleaning.Clean(labeled, fallback: "fallback");

        Assert.Equal("Clean text", result);
    }

    [Fact]
    public void FallsBackWhenNothingIsLeftAfterCleaning()
    {
        var result = OutputCleaning.Clean("Output:", fallback: "the original transcript");

        Assert.Equal("the original transcript", result);
    }

    [Fact]
    public void FallsBackOnWhitespaceOnlyOutput()
    {
        var result = OutputCleaning.Clean("   \n  ", fallback: "the original transcript");

        Assert.Equal("the original transcript", result);
    }
}
