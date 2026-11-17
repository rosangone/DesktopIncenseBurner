using System;
using System.Windows;
using System.Windows.Controls;

namespace DesktopIncenseBurner;

public partial class SettingsWindow : Window
{
    public IncenseSettings Result { get; private set; }

    public SettingsWindow(IncenseSettings current)
    {
        InitializeComponent();
        Result = new IncenseSettings
        {
            WindowWidth = current.WindowWidth,
            WindowHeight = current.WindowHeight,
            WindowOpacity = current.WindowOpacity,
            AlwaysOnTop = current.AlwaysOnTop,
            SmokeDensity = current.SmokeDensity,
            SmokeSpeed = current.SmokeSpeed,
            IncenseCount = current.IncenseCount,
            BurnMinutes = current.BurnMinutes,
            PlaySoundOnFinish = current.PlaySoundOnFinish
        };

        CountBox.SelectedIndex = Math.Clamp(current.IncenseCount, 1, 3) - 1;
        MinutesBox.Text = current.BurnMinutes.ToString();
        DensitySlider.Value = current.SmokeDensity;
        SpeedSlider.Value = current.SmokeSpeed;
        OpacitySlider.Value = current.WindowOpacity;
        TopmostCheck.IsChecked = current.AlwaysOnTop;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinutesBox.Text, out int minutes) || minutes < 1 || minutes > 1440)
        {
            MessageBox.Show("燃烧时间请输入 1 到 1440 之间的整数。", "输入有误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.IncenseCount = CountBox.SelectedIndex + 1;
        Result.BurnMinutes = minutes;
        Result.SmokeDensity = (int)DensitySlider.Value;
        Result.SmokeSpeed = SpeedSlider.Value;
        Result.WindowOpacity = OpacitySlider.Value;
        Result.AlwaysOnTop = TopmostCheck.IsChecked == true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
