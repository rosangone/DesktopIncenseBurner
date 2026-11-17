using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DesktopIncenseBurner;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _animationTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DispatcherTimer _burnTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Random _random = new();
    private readonly List<SmokeParticle> _smoke = new();
    private IncenseSettings _settings = SettingsStore.Load();
    private TimeSpan _remaining;
    private bool _isBurning;
    private bool _isPaused;

    public MainWindow()
    {
        InitializeComponent();
        Width = Math.Clamp(_settings.WindowWidth, 260, 900);
        Height = Math.Clamp(_settings.WindowHeight, 300, 900);
        Opacity = Math.Clamp(_settings.WindowOpacity, 0.35, 1.0);
        Topmost = !_settings.DesktopLayerMode && _settings.AlwaysOnTop;
        _animationTimer.Tick += (_, _) => AnimateScene();
        _burnTimer.Tick += (_, _) => UpdateBurnTimer();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        DrawBurner();
        _animationTimer.Start();
        UpdateStatus();
        SendToDesktopLayer();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded) DrawBurner();
    }

    private void SendToDesktopLayer()
    {
        // Desktop mode is a normal non-topmost window: other applications cover it,
        // while it remains visible on the desktop when no other window is in front.
        if (_settings.DesktopLayerMode) Topmost = false;
    }

    private void Window_Deactivated(object? sender, EventArgs e) => SendToDesktopLayer();

    private void DrawBurner()
    {
        if (!IsLoaded) return;
        SceneCanvas.Children.Clear();
        _smoke.Clear();

        double w = Math.Max(ActualWidth, 260);
        double h = Math.Max(ActualHeight, 300);
        double scale = Math.Min(w / 360.0, h / 420.0);
        double cx = w / 2;
        double baseY = h * 0.73;

        var halo = new Ellipse
        {
            Width = 230 * scale, Height = 150 * scale,
            Fill = new RadialGradientBrush(Color.FromArgb(52, 226, 174, 83), Colors.Transparent)
        };
        Canvas.SetLeft(halo, cx - halo.Width / 2);
        Canvas.SetTop(halo, baseY - 160 * scale);
        SceneCanvas.Children.Add(halo);

        int count = Math.Clamp(_settings.IncenseCount, 1, 3);
        double[] offsets = count switch
        {
            1 => new[] { 0.0 },
            2 => new[] { -9.0, 9.0 },
            _ => new[] { -18.0, 0.0, 18.0 }
        };
        foreach (double offset in offsets)
        {
            var stick = new Line
            {
                X1 = cx + offset * scale, Y1 = baseY - 48 * scale,
                X2 = cx + (offset - 5) * scale, Y2 = baseY - 166 * scale,
                Stroke = new SolidColorBrush(Color.FromRgb(184, 116, 55)),
                StrokeThickness = 3.2 * scale, StrokeStartLineCap = PenLineCap.Round
            };
            SceneCanvas.Children.Add(stick);
            var ember = new Ellipse
            {
                Width = 5 * scale, Height = 7 * scale,
                Fill = new RadialGradientBrush(Color.FromRgb(255, 230, 135), Color.FromRgb(232, 76, 27))
            };
            Canvas.SetLeft(ember, cx + (offset - 7) * scale);
            Canvas.SetTop(ember, baseY - 171 * scale);
            SceneCanvas.Children.Add(ember);
        }

        // 对称炉耳、三足与厚重的古铜金炉身
        AddPath("M 0,8 C -18,0 -25,12 -22,29 C -18,42 -8,39 0,31",
            cx - 78 * scale, baseY - 40 * scale, scale, "#9C642E", "#E5BC70", 2);
        AddPath("M 0,8 C 18,0 25,12 22,29 C 18,42 8,39 0,31",
            cx + 78 * scale, baseY - 40 * scale, scale, "#9C642E", "#E5BC70", 2);
        AddPath("M 0,0 C -5,12 -12,18 -20,20 L 20,20 C 12,18 5,12 0,0 Z",
            cx - 49 * scale, baseY + 31 * scale, scale, "#68401F", "#C18B48", 1.5);
        AddPath("M 0,0 C -5,12 -12,18 -20,20 L 20,20 C 12,18 5,12 0,0 Z",
            cx + 29 * scale, baseY + 31 * scale, scale, "#68401F", "#C18B48", 1.5);

        AddPath("M 0,0 C 8,39 30,69 82,74 C 134,69 156,39 164,0 Z",
            cx - 82 * scale, baseY - 34 * scale, scale, "#8E5528", "#E6BE78", 2.2,
            new LinearGradientBrush(Color.FromRgb(235, 190, 112), Color.FromRgb(83, 43, 24), 90));
        AddEllipse(cx - 79 * scale, baseY - 38 * scale, 158 * scale, 17 * scale, "#B27A3D", "#F0CE8B", 1.8);
        AddEllipse(cx - 70 * scale, baseY + 32 * scale, 140 * scale, 13 * scale, "#744522", "#D7A45D", 1.5);
        AddEllipse(cx - 55 * scale, baseY + 41 * scale, 110 * scale, 10 * scale, "#50301D", "#B98445", 1.2);

        AddText("回", cx - 65 * scale, baseY - 4 * scale, 24 * scale, "#F0C879", 17 * scale);
        AddText("回", cx + 42 * scale, baseY - 4 * scale, 24 * scale, "#F0C879", 17 * scale);
        AddText("✦", cx - 7 * scale, baseY - 30 * scale, 14 * scale, "#F6D58E", 12 * scale);

        var plate = new Border
        {
            Width = 104 * scale, Height = 29 * scale,
            CornerRadius = new CornerRadius(5 * scale),
            Background = new LinearGradientBrush(Color.FromRgb(91, 48, 24), Color.FromRgb(135, 79, 35), 90),
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 181, 105)),
            BorderThickness = new Thickness(1 * scale),
            Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(_settings.Inscription) ? "招财进宝" : _settings.Inscription,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 220, 148)),
                FontFamily = new FontFamily("KaiTi"),
                FontSize = 15 * scale,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            }
        };
        Canvas.SetLeft(plate, cx - plate.Width / 2);
        Canvas.SetTop(plate, baseY + 1 * scale);
        SceneCanvas.Children.Add(plate);

        AddEllipse(cx - 75 * scale, baseY - 54 * scale, 150 * scale, 19 * scale, "#A96E35", "#F0C982", 1.8);
        AddPath("M 0,12 C 4,0 16,0 20,12 L 18,19 L 2,19 Z",
            cx - 10 * scale, baseY - 72 * scale, scale, "#B98242", "#F0CC87", 1.3);
        var finial = new Ellipse
        {
            Width = 12 * scale, Height = 12 * scale,
            Fill = new RadialGradientBrush(Color.FromRgb(255, 224, 143), Color.FromRgb(137, 77, 30)),
            Stroke = new SolidColorBrush(Color.FromRgb(245, 207, 133)),
            StrokeThickness = 1 * scale
        };
        Canvas.SetLeft(finial, cx - 6 * scale);
        Canvas.SetTop(finial, baseY - 81 * scale);
        SceneCanvas.Children.Add(finial);
    }

    private void AddPath(string geometry, double left, double top, double scale,
        string fallbackFill, string stroke, double thickness, Brush? fill = null)
    {
        var shape = new Path
        {
            Data = Geometry.Parse(geometry),
            Fill = fill ?? (Brush)new BrushConverter().ConvertFromString(fallbackFill)!,
            Stroke = (Brush)new BrushConverter().ConvertFromString(stroke)!,
            StrokeThickness = thickness
        };
        var transforms = new TransformGroup();
        transforms.Children.Add(new ScaleTransform(scale, scale));
        transforms.Children.Add(new TranslateTransform(left, top));
        shape.RenderTransform = transforms;
        SceneCanvas.Children.Add(shape);
    }

    private void AddText(string value, double left, double top, double width, string color, double size)
    {
        var label = new TextBlock
        {
            Text = value, Width = width, FontSize = size,
            Foreground = (Brush)new BrushConverter().ConvertFromString(color)!,
            FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center
        };
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        SceneCanvas.Children.Add(label);
    }

    private void AddEllipse(double left, double top, double width, double height, string fill, string stroke, double thickness)
    {
        var shape = new Ellipse
        {
            Width = width, Height = height,
            Fill = (Brush)new BrushConverter().ConvertFromString(fill)!,
            Stroke = (Brush)new BrushConverter().ConvertFromString(stroke)!,
            StrokeThickness = thickness
        };
        Canvas.SetLeft(shape, left);
        Canvas.SetTop(shape, top);
        SceneCanvas.Children.Add(shape);
    }

    private void AnimateScene()
    {
        if (!IsLoaded) return;
        for (int i = SceneCanvas.Children.Count - 1; i >= 0; i--)
            if (SceneCanvas.Children[i] is Ellipse el && el.Tag as string == "smoke")
                SceneCanvas.Children.RemoveAt(i);

        if (_isBurning && !_isPaused)
        {
            int spawn = Math.Clamp(_settings.SmokeDensity / 15, 1, 6);
            double scale = Math.Min(ActualWidth / 360.0, ActualHeight / 420.0);
            for (int i = 0; i < spawn; i++)
            {
                _smoke.Add(new SmokeParticle
                {
                    X = ActualWidth / 2 + _random.NextDouble() * 24 * scale - 12 * scale,
                    Y = ActualHeight * 0.73 - 178 * scale,
                    Radius = (5 + _random.NextDouble() * 9) * scale,
                    VelocityX = (_random.NextDouble() - 0.5) * 0.65 * scale,
                    VelocityY = -(0.45 + _random.NextDouble() * 0.7) * _settings.SmokeSpeed * scale,
                    Life = 1.0
                });
            }
        }

        for (int i = _smoke.Count - 1; i >= 0; i--)
        {
            var p = _smoke[i];
            p.X += p.VelocityX;
            p.Y += p.VelocityY;
            p.Radius += 0.10;
            p.Life -= 0.012;
            if (p.Life <= 0 || p.Y < 20) { _smoke.RemoveAt(i); continue; }

            var cloud = new Ellipse
            {
                Width = p.Radius * 2, Height = p.Radius * 2,
                Fill = new SolidColorBrush(Color.FromArgb((byte)(Math.Clamp(p.Life, 0, 1) * 48), 215, 211, 204)),
                Tag = "smoke"
            };
            Canvas.SetLeft(cloud, p.X - p.Radius);
            Canvas.SetTop(cloud, p.Y - p.Radius);
            SceneCanvas.Children.Add(cloud);
        }
    }

    private void LightIncense_Click(object sender, RoutedEventArgs e)
    {
        _settings.IncenseCount = Math.Clamp(_settings.IncenseCount, 1, 3);
        _remaining = TimeSpan.FromMinutes(Math.Clamp(_settings.BurnMinutes, 1, 1440));
        _isBurning = true;
        _isPaused = false;
        _burnTimer.Start();
        PauseButton.Content = "暂停";
        DrawBurner();
        UpdateStatus();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBurning) return;
        _isPaused = !_isPaused;
        PauseButton.Content = _isPaused ? "继续" : "暂停";
        UpdateStatus();
    }

    private void UpdateBurnTimer()
    {
        if (!_isBurning || _isPaused) return;
        _remaining -= TimeSpan.FromSeconds(1);
        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            _isBurning = false;
            _burnTimer.Stop();
            StatusText.Text = "本次燃香已结束";
            TimerText.Text = "香已燃尽";
            if (_settings.PlaySoundOnFinish) System.Media.SystemSounds.Asterisk.Play();
            return;
        }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (!_isBurning)
        {
            StatusText.Text = "香火安宁";
            TimerText.Text = "尚未上香";
            return;
        }
        StatusText.Text = _isPaused ? "燃香已暂停" : "香火正在燃烧";
        TimerText.Text = $"剩余时间  {(int)_remaining.TotalHours:00}{_remaining:mm\\:ss}";
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Result;
            Width = Math.Clamp(_settings.WindowWidth, 260, 900);
            Height = Math.Clamp(_settings.WindowHeight, 300, 900);
            Opacity = Math.Clamp(_settings.WindowOpacity, 0.35, 1.0);
            Topmost = _settings.AlwaysOnTop;
            _settings.DesktopLayerMode = !_settings.AlwaysOnTop;
            SettingsStore.Save(_settings);
            DrawBurner();
            SendToDesktopLayer();
        }
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Clamp(Width + e.HorizontalChange, 260, 900);
        Height = Math.Clamp(Height + e.VerticalChange, 300, 900);
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
    }

    private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button || e.OriginalSource is Thumb) return;
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); } catch (InvalidOperationException) { }
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.WindowOpacity = Opacity;
        _settings.AlwaysOnTop = Topmost;
        _settings.DesktopLayerMode = !Topmost;
        SettingsStore.Save(_settings);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class SmokeParticle
    {
        public double X, Y, Radius, VelocityX, VelocityY, Life;
    }
}
