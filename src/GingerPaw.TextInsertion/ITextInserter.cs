namespace GingerPaw.TextInsertion;

/// <summary>Mirrors the Mac app's TextInserter protocol: set clipboard, optionally send paste.</summary>
public interface ITextInserter
{
    Task<InsertionOutcome> InsertAsync(string text, bool restoreClipboard);
    Task<InsertionOutcome> CopyAsync(string text);
}

public enum InsertionOutcome
{
    Pasted,
    Copied,
    Failed
}
