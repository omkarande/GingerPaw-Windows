using System.Windows;

namespace GingerPaw.Overlay;

/// <summary>
/// Port of the Mac pill's positioning math (DictationOverlayController.position):
/// horizontally centered, sitting a fixed margin above the screen's bottom edge.
/// Kept as a pure function (no live Window/Screen dependency) so it's unit-testable,
/// per plan.md's "overlay positioning math as a pure function" testing-strategy item.
/// </summary>
public static class OverlayPositioning
{
    private const double BottomMargin = 32;

    public static Point ComputeTopLeft(Rect workArea, Size pillSize)
    {
        var x = workArea.Left + (workArea.Width - pillSize.Width) / 2;
        var y = workArea.Bottom - pillSize.Height - BottomMargin;
        return new Point(x, y);
    }
}
