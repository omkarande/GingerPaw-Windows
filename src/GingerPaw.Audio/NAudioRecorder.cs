using NAudio.Wave;

namespace GingerPaw.Audio;

/// <summary>
/// Captures the default mic and writes 16kHz mono 16-bit PCM WAV, matching the Mac app's
/// AVAudioRecorder settings so Whisper sees the same input shape on both platforms.
/// Uses WaveInEvent and asks for the target format directly — Windows' audio stack does
/// the sample-rate/channel conversion, so no manual resampler chain is needed.
/// </summary>
public sealed class NAudioRecorder : IAudioRecording
{
    private static readonly WaveFormat TargetFormat = new(16_000, 16, 1);

    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _outputPath;

    public void Start()
    {
        if (_waveIn is not null)
        {
            throw new AudioRecordingException("Already recording.");
        }

        var path = Path.Combine(Path.GetTempPath(), $"gingerpaw-{Guid.NewGuid():N}.wav");
        var writer = new WaveFileWriter(path, TargetFormat);
        var waveIn = new WaveInEvent { WaveFormat = TargetFormat };

        waveIn.DataAvailable += (_, e) => writer.Write(e.Buffer, 0, e.BytesRecorded);

        _waveIn = waveIn;
        _writer = writer;
        _outputPath = path;

        waveIn.StartRecording();
    }

    public string Stop()
    {
        var path = StopCapture();
        if (path is null)
        {
            throw new AudioRecordingException("Not recording.");
        }
        return path;
    }

    public void Cancel()
    {
        var path = StopCapture();
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string? StopCapture()
    {
        if (_waveIn is null || _outputPath is null || _writer is null)
        {
            return null;
        }

        // StopRecording() only requests a stop — the capture thread may still be
        // draining buffered DataAvailable writes when it returns. Wait for
        // RecordingStopped to confirm the thread is done, then close the writer
        // ourselves explicitly rather than relying on an event-handler subscription
        // order to do it (that approach silently left the file handle open forever
        // here, since RecordingStopped wasn't observed to fire reliably).
        using var stopped = new ManualResetEventSlim(false);
        void OnStopped(object? s, StoppedEventArgs e) => stopped.Set();
        _waveIn.RecordingStopped += OnStopped;
        _waveIn.StopRecording();
        var signaled = stopped.Wait(TimeSpan.FromSeconds(2));
        _waveIn.RecordingStopped -= OnStopped;
        if (!signaled)
        {
            Console.WriteLine($"{DateTime.Now:O} [NAudioRecorder] WARNING: RecordingStopped did not fire within 2s; closing writer anyway.");
        }

        _writer.Dispose();
        _waveIn.Dispose();

        var path = _outputPath;
        _waveIn = null;
        _writer = null;
        _outputPath = null;
        return path;
    }
}
