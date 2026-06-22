using System.Text.Json;

namespace GingerPaw.Settings;

/// <summary>
/// JSON-backed settings store, mirroring the Mac app's FlowSettings: same defaults
/// (FormatEnabled off, AutoPaste/RestoreClipboard/ShowPill on), persisted to
/// %AppData%\GingerPaw\settings.json instead of UserDefaults. Each setter saves
/// immediately, matching FlowSettings' didSet-triggered persistence.
/// </summary>
public sealed class GingerPawSettings
{
    private readonly string _path;
    private readonly SettingsData _data;

    public GingerPawSettings(string? path = null)
    {
        _path = path ?? DefaultPath;
        _data = Load(_path);
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GingerPaw",
        "settings.json");

    public string ModelId
    {
        get => _data.ModelId;
        set { _data.ModelId = value; Save(); }
    }

    public bool AutoPaste
    {
        get => _data.AutoPaste;
        set { _data.AutoPaste = value; Save(); }
    }

    public bool RestoreClipboard
    {
        get => _data.RestoreClipboard;
        set { _data.RestoreClipboard = value; Save(); }
    }

    public bool ShowPill
    {
        get => _data.ShowPill;
        set { _data.ShowPill = value; Save(); }
    }

    public bool FormatEnabled
    {
        get => _data.FormatEnabled;
        set { _data.FormatEnabled = value; Save(); }
    }

    private static SettingsData Load(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data is not null)
            {
                return data;
            }
        }
        return new SettingsData();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(_path, JsonSerializer.Serialize(_data));
    }

    private sealed class SettingsData
    {
        public string ModelId { get; set; } = "ggml-base";
        public bool AutoPaste { get; set; } = true;
        public bool RestoreClipboard { get; set; } = true;
        public bool ShowPill { get; set; } = true;
        public bool FormatEnabled { get; set; } = false;
    }
}
