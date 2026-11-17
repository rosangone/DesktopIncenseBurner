using System.IO;
using System.Text.Json;

namespace DesktopIncenseBurner;

public static class SettingsStore
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "DesktopIncenseBurner");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static IncenseSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<IncenseSettings>(json) ?? new IncenseSettings();
            }
        }
        catch
        {
            // A damaged settings file should not prevent the application from starting.
        }
        return new IncenseSettings();
    }

    public static void Save(IncenseSettings settings)
    {
        Directory.CreateDirectory(Folder);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
