namespace DesktopIncenseBurner;

public sealed class IncenseSettings
{
    public double WindowWidth { get; set; } = 360;
    public double WindowHeight { get; set; } = 420;
    public double WindowOpacity { get; set; } = 1.0;
    public bool AlwaysOnTop { get; set; } = false;
    public bool DesktopLayerMode { get; set; } = true;
    public int SmokeDensity { get; set; } = 45;
    public double SmokeSpeed { get; set; } = 1.0;
    public int IncenseCount { get; set; } = 1;
    public int BurnMinutes { get; set; } = 60;
    public bool PlaySoundOnFinish { get; set; } = false;
    public string Inscription { get; set; } = "招财进宝";
}
