using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GingerPaw.Overlay;

/// <summary>Which glyph the pill's icon slot shows — mirrors the Mac pill's icon+paw swap.</summary>
public enum PillIcon
{
    Mic,
    Paw,
    Bars,
    Download,
    Checkmark,
    Warning
}

/// <summary>
/// The borderless floating pill window. Owns its own visuals (icon/title/color/animations);
/// GingerPaw.Overlay.OverlayController owns lifecycle, visibility, and state->visual mapping.
/// </summary>
public partial class PillOverlayWindow : Window
{
    // Segoe MDL2 Assets codepoints (built into Windows 10/11) — the Windows analog of the
    // Mac pill's SF Symbols, which are also referenced by code point rather than image files.
    private const string GlyphMic = "";
    private const string GlyphDownload = "";
    private const string GlyphCheckmark = "";
    private const string GlyphWarning = "";

    private static readonly Color RecordingColor = Color.FromRgb(0xFF, 0x3B, 0x30);
    private static readonly Color DefaultColor = Color.FromArgb(225, 0x14, 0x12, 0x1E); // ~88% alpha, same as Mac

    public PillOverlayWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Reposition();
    }

    public void Apply(string title, PillIcon icon, bool danger)
    {
        TitleText.Text = title;
        Pill.Background = new SolidColorBrush(danger ? RecordingColor : DefaultColor);

        IconGlyph.Visibility = Visibility.Collapsed;
        PawIcon.Visibility = Visibility.Collapsed;
        BarsIcon.Visibility = Visibility.Collapsed;
        StopBarsPulse();

        if (icon == PillIcon.Paw)
        {
            PawIcon.Visibility = Visibility.Visible;
        }
        else if (icon == PillIcon.Bars)
        {
            BarsIcon.Visibility = Visibility.Visible;
            StartBarsPulse();
        }
        else
        {
            IconGlyph.Visibility = Visibility.Visible;
            IconGlyph.Text = icon switch
            {
                PillIcon.Download => GlyphDownload,
                PillIcon.Checkmark => GlyphCheckmark,
                PillIcon.Warning => GlyphWarning,
                _ => GlyphMic
            };
        }
    }

    public void StartPurr()
    {
        var purr = new DoubleAnimation
        {
            From = 1.0,
            To = 1.02,
            Duration = TimeSpan.FromSeconds(0.5),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        PillScale.BeginAnimation(ScaleTransform.ScaleXProperty, purr);
        PillScale.BeginAnimation(ScaleTransform.ScaleYProperty, purr);
    }

    public void StopPurr()
    {
        PillScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        PillScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        PillScale.ScaleX = 1.0;
        PillScale.ScaleY = 1.0;
    }

    /// <summary>One-shot emphasis animation, used for both Copied (Mac parity) and Failed
    /// (Windows-only divergence — makes the short Failed flash actually noticeable).</summary>
    public void PlayBounce()
    {
        var bounce = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(280),
            KeyFrames =
            {
                new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0)),
                new LinearDoubleKeyFrame(1.18, KeyTime.FromPercent(0.5)),
                new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(1))
            }
        };
        PillScale.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
        PillScale.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
    }

    private void StartBarsPulse()
    {
        AnimateBar(Bar1, 0.0);
        AnimateBar(Bar2, 0.15);
        AnimateBar(Bar3, 0.3);
    }

    private void StopBarsPulse()
    {
        foreach (var bar in new[] { Bar1, Bar2, Bar3 })
        {
            bar.BeginAnimation(OpacityProperty, null);
            bar.Opacity = 0.4;
        }
    }

    private static void AnimateBar(Rectangle bar, double beginSeconds)
    {
        var pulse = new DoubleAnimation
        {
            From = 0.4,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(0.35),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds)
        };
        bar.BeginAnimation(OpacityProperty, pulse);
    }

    /// <summary>Re-centers bottom-of-screen whenever content (and so SizeToContent's
    /// measured size) changes — e.g. a longer/shorter title swapping in.</summary>
    private void Reposition()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        var topLeft = OverlayPositioning.ComputeTopLeft(SystemParameters.WorkArea, new Size(ActualWidth, ActualHeight));
        Left = topLeft.X;
        Top = topLeft.Y;
    }
}
