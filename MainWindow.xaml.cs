using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        Opacity = Math.Clamp(_settings.WindowOpacity, 0.35, 1.0);
        Topmost = _settings.AlwaysOnTop;
        _animationTimer.Tick += (_, _) => AnimateScene();
        _burnTimer.Tick += (_, _) => UpdateBurnTimer();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        DrawBurner();
        _animationTimer.Start();
        UpdateStatus();
    }

    private void DrawBurner()
    {
        SceneCanvas.Children.Clear();
        _smoke.Clear();
        double w = Math.Max(Width, 260);
        double h = Math.Max(Height, 300);
        double cx = w / 2;
        double baseY = h * 0.72;

        var halo = new Ellipse
        {
            Width = w * 0.72, Height = h * 0.42,
            Fill = new RadialGradientBrush(Color.FromArgb(45, 219, 155, 83), Colors.Transparent)
        };
        Canvas.SetLeft(halo, cx - halo.Width / 2);
        Canvas.SetTop(halo, h * 0.35);
        SceneCanvas.Children.Add(halo);

        int count = Math.Clamp(_settings.IncenseCount, 1, 3);
        double[] offsets = count switch
        {
            1 => new[] { 0.0 },
            2 => new[] { -9.0, 9.0 },
            _ => new[] { -17.0, 0.0, 17.0 }
        };
        foreach (double offset in offsets)
        {
            var stick = new Line
            {
                X1 = cx + offset, Y1 = baseY - 34,
                X2 = cx + offset - 4, Y2 = baseY - 145,
                Stroke = new SolidColorBrush(Color.FromRgb(179, 112, 57)),
                StrokeThickness = 3.2, StrokeStartLineCap = PenLineCap.Round
            };
            SceneCanvas.Children.Add(stick);
            var ember = new Ellipse { Width = 5, Height = 7, Fill = new SolidColorBrush(Color.FromRgb(255, 104, 42)) };
            Canvas.SetLeft(ember, cx + offset - 6.5);
            Canvas.SetTop(ember, baseY - 150);
            SceneCanvas.Children.Add(ember);
        }

        AddEllipse(cx - 66, baseY - 48, 132, 22, "#D6A45F", "#6D421F", 2);
        var body = new Path
        {
            Data = Geometry.Parse("M 0,0 C 8,48 34,65 66,65 C 98,65 124,48 132,0 Z"),
            Fill = new LinearGradientBrush(Color.FromRgb(197, 139, 69), Color.FromRgb(91, 49, 27), 90),
            Stroke = new SolidColorBrush(Color.FromRgb(224, 177, 105)),
            StrokeThickness = 2
        };
        Canvas.SetLeft(body, cx - 66);
        Canvas.SetTop(body, baseY - 39);
        SceneCanvas.Children.Add(body);
        AddEllipse(cx - 59, baseY - 42, 118, 13, "#A66C35", "#E0B574", 1);
        AddEllipse(cx - 34, baseY + 22, 68, 12, "#8B542D", "#D7A15C", 1);
        AddEllipse(cx - 24, baseY + 30, 48, 9, "#5C351F", "#B77C42", 1);
        var ornament = new TextBlock { Text = "✦", Foreground = new SolidColorBrush(Color.FromRgb(244, 201, 132)), FontSize = 22 };
        Canvas.SetLeft(ornament, cx - 11);
        Canvas.SetTop(ornament, baseY - 20);
        SceneCanvas.Children.Add(ornament);
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
            for (int i = 0; i < spawn; i++)
            {
                _smoke.Add(new SmokeParticle
                {
                    X = ActualWidth / 2 + _random.NextDouble() * 24 - 12,
                    Y = ActualHeight * 0.72 - 155,
                    Radius = 5 + _random.NextDouble() * 9,
                    VelocityX = (_random.NextDouble() - 0.5) * 0.65,
                    VelocityY = -(0.45 + _random.NextDouble() * 0.7) * _settings.SmokeSpeed,
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
            System.Media.SystemSounds.Asterisk.Play();
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
            Width = _settings.WindowWidth;
            Height = _settings.WindowHeight;
            Opacity = Math.Clamp(_settings.WindowOpacity, 0.35, 1.0);
            Topmost = _settings.AlwaysOnTop;
            SettingsStore.Save(_settings);
            DrawBurner();
        }
    }

    private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
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
        SettingsStore.Save(_settings);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class SmokeParticle
    {
        public double X, Y, Radius, VelocityX, VelocityY, Life;
    }
}
