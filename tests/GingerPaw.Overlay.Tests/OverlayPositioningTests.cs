using System.Windows;
using GingerPaw.Overlay;

namespace GingerPaw.Overlay.Tests;

public class OverlayPositioningTests
{
    [Fact]
    public void CentersHorizontallyOnPrimaryWorkArea()
    {
        var workArea = new Rect(0, 0, 1920, 1040);
        var pillSize = new Size(200, 44);

        var topLeft = OverlayPositioning.ComputeTopLeft(workArea, pillSize);

        Assert.Equal((1920 - 200) / 2, topLeft.X);
    }

    [Fact]
    public void SitsThirtyTwoPixelsAboveWorkAreaBottom()
    {
        var workArea = new Rect(0, 0, 1920, 1040);
        var pillSize = new Size(200, 44);

        var topLeft = OverlayPositioning.ComputeTopLeft(workArea, pillSize);

        Assert.Equal(1040 - 44 - 32, topLeft.Y);
    }

    [Fact]
    public void AccountsForNonZeroWorkAreaOrigin()
    {
        // Mirrors a secondary-monitor-shaped work area to make sure the math
        // isn't hardcoded to assume the primary screen starts at (0,0).
        var workArea = new Rect(1920, 100, 1280, 800);
        var pillSize = new Size(160, 44);

        var topLeft = OverlayPositioning.ComputeTopLeft(workArea, pillSize);

        Assert.Equal(1920 + (1280 - 160) / 2, topLeft.X);
        Assert.Equal(100 + 800 - 44 - 32, topLeft.Y);
    }
}
