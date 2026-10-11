using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mimo.Audio;
using Mimo.Controls;
using Mimo.Core;
using Mimo.Models;
using WpfPath = System.Windows.Shapes.Path;

namespace Mimo;

public partial class MainWindow : Window
{
    private const double MascotAreaHeight = 132;
    private const double PanelHeight = 46;
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly Geometry PlayGeometry = Geometry.Parse("M 3,2 L 12,8 L 3,14 Z");
    private static readonly Geometry PauseGeometry = Geometry.Parse("M 3,2 H 5.5 V 14 H 3 Z M 9.5,2 H 12 V 14 H 9.5 Z");
    private readonly AppState _state;
    private readonly PomodoroTimer _timer;
    private readonly CompletionSounds _completionSounds = new();
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _saveDebounce = new() { Interval = TimeSpan.FromMilliseconds(550) };
    private readonly List<Button> _mascotButtons = [];
    private bool _ready;
    private bool _changingLayout;
    private bool _loadingSettings;
    private Point? _dragOrigin;
    private Point _contextMenuPosition;

    public MainWindow()
    {
        _state = AppState.Load();
        _timer = new PomodoroTimer
        {
            FocusMinutes = _state.FocusMinutes,
            ShortBreakMinutes = _state.ShortBreakMinutes,
            LongBreakMinutes = _state.LongBreakMinutes,
            LongBreakEvery = _state.LongBreakEvery
        };
        _state.FocusMinutes = _timer.FocusMinutes;
        _state.ShortBreakMinutes = _timer.ShortBreakMinutes;
        _state.LongBreakMinutes = _timer.LongBreakMinutes;
        _state.LongBreakEvery = _timer.LongBreakEvery;

        InitializeComponent();
        _state.SelectedMascot = 0;
        for (var i = 0; i < AnimatedMascot.Mascots.Count; i++)
            if (AnimatedMascot.Mascots[i].Id == _state.SelectedMascotId)
            {
                _state.SelectedMascot = i;
                break;
            }
        BuildMascotPicker();
        SelectMascot(_state.SelectedMascot, save: false);
        Root.ContextMenu = BuildContextMenu();
        Root.ContextMenuOpening += (_, _) =>
        {
            _contextMenuPosition = Mouse.GetPosition(Root);
            Root.ContextMenu = BuildContextMenu();
        };
        _ticker.Tick += (_, _) => { _timer.Tick(DateTimeOffset.Now); UpdateDisplay(); };
        _timer.SessionFinished += _completionSounds.Play;
        _saveDebounce.Tick += (_, _) => { _saveDebounce.Stop(); SaveState(); };
        _ready = true;
        UpdateDisplay();
        _ticker.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _changingLayout = true;
        try
        {
            var hasSavedPosition = false;
            if (_state.WindowLeft is double x && _state.WindowTop is double y &&
                double.IsFinite(x) && double.IsFinite(y))
            {
                Left = x;
                Top = y;
                hasSavedPosition = true;
            }
            if (!hasSavedPosition || !PositionIsOnScreen())
            {
                var work = SystemParameters.WorkArea;
                Left = work.Right - Width - 44;
                Top = work.Bottom - Height - 42;
            }

            if (_state.MascotHidden)
                SetMascotVisible(false);
        }
        finally { _changingLayout = false; }
        KeepTimerPanelOnScreen();
        RememberPosition();
    }

    private bool PositionIsOnScreen()
    {
        var topLeft = PointToScreen(new Point(0, 0));
        var bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
        var windowBounds = new Rect(topLeft, bottomRight);
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var area = screen.WorkingArea;
            var visible = Rect.Intersect(windowBounds,
                new Rect(area.Left, area.Top, area.Width, area.Height));
            if (visible.Width >= 30 && visible.Height >= 30)
                return true;
        }
        return false;
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (!_ready || !IsLoaded || _changingLayout) return;
        KeepTimerPanelOnScreen();
        RememberPosition();
    }

    private void KeepTimerPanelOnScreen()
    {
        if (!IsLoaded || TimerView.ActualWidth <= 0 || TimerView.ActualHeight <= 0) return;

        var topLeft = TimerView.PointToScreen(new Point(0, 0));
        var bottomRight = TimerView.PointToScreen(new Point(TimerView.ActualWidth, TimerView.ActualHeight));
        var center = new System.Drawing.Point(
            (int)Math.Round((topLeft.X + bottomRight.X) / 2),
            (int)Math.Round((topLeft.Y + bottomRight.Y) / 2));
        var area = System.Windows.Forms.Screen.FromPoint(center).WorkingArea;
        const double margin = 4;
        var targetX = Math.Clamp(topLeft.X, area.Left + margin,
            area.Right - margin - (bottomRight.X - topLeft.X));
        var targetY = Math.Clamp(topLeft.Y, area.Top + margin,
            area.Bottom - margin - (bottomRight.Y - topLeft.Y));
        if (Math.Abs(targetX - topLeft.X) < 0.5 && Math.Abs(targetY - topLeft.Y) < 0.5) return;

        var current = PointFromScreen(topLeft);
        var corrected = PointFromScreen(new Point(targetX, targetY));
        _changingLayout = true;
        try
        {
            Left += corrected.X - current.X;
            Top += corrected.Y - current.Y;
        }
        finally { _changingLayout = false; }
    }

    private void RememberPosition()
    {
        if (!_ready) return;
        _state.WindowLeft = Left;
        _state.WindowTop = Top - (_state.MascotHidden ? MascotAreaHeight : 0);
        ScheduleSave();
    }

    private void Mascot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => BeginDrag(e);

    private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsInsideInteractiveControl(source)) return;
        BeginDrag(e);
    }

    private void BeginDrag(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _dragOrigin = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragOrigin is not Point origin) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            CancelDrag();
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - origin.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - origin.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        CancelDrag();
        Mouse.OverrideCursor = Cursors.Hand;
        try { DragMove(); }
        catch (InvalidOperationException) { }
        finally
        {
            Mouse.OverrideCursor = null;
            RememberPosition();
        }
    }

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => CancelDrag();

    private void CancelDrag()
    {
        _dragOrigin = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private static bool IsInsideInteractiveControl(DependencyObject element)
    {
        for (var current = element; current is not null;
             current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current))
            if (current is ButtonBase or TextBox or ScrollViewer) return true;
        return false;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.IsRunning) _timer.Pause(DateTimeOffset.Now);
        else _timer.Start(DateTimeOffset.Now);
        UpdateDisplay();
    }

    private void NextPhaseButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Skip(DateTimeOffset.Now);
        UpdateDisplay();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Reset();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!_ready) return;
        var seconds = Math.Max(0, (int)Math.Ceiling(_timer.Remaining.TotalSeconds));
        TimeLabel.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        PhaseLabel.Text = _timer.Kind switch
        {
            SessionKind.Focus => $"{_timer.CurrentRound}. TUR",
            SessionKind.ShortBreak => "KISA MOLA",
            _ => "UZUN MOLA"
        };
        PhaseLabel.Foreground = _timer.Kind == SessionKind.Focus
            ? BrushFrom("#F8B77D") : BrushFrom("#9ED7C8");
        PlayGlyph.Data = _timer.IsRunning ? PauseGeometry : PlayGeometry;
        PlayButton.ToolTip = _timer.IsRunning ? "Duraklat" : _timer.Progress > 0 ? "Devam et" : "Başlat";
        MascotImage.SetAnimation(_timer.Kind switch
        {
            SessionKind.Focus => "focus",
            SessionKind.ShortBreak => "shortBreak",
            _ => "longBreak"
        });
        MascotImage.SetPlaying(_timer.IsRunning);
        Title = $"Mimo · {TimeLabel.Text} {PhaseLabel.Text.ToLower(Turkish)}";
    }

    private void SelectMascot(int index, bool save = true)
    {
        var count = AnimatedMascot.Mascots.Count;
        _state.SelectedMascot = (index % count + count) % count;
        var mascot = AnimatedMascot.Mascots[_state.SelectedMascot];
        _state.SelectedMascotId = mascot.Id;
        MascotImage.SetMascot(mascot.Id);
        MascotImage.ToolTip = $"{mascot.Name} · Taşımak için sürükle";
        UpdateMascotSelection();
        if (save)
            SaveState();
    }

    private void BuildMascotPicker()
    {
        var style = (Style)FindResource("MascotThumbButtonStyle");
        for (var i = 0; i < AnimatedMascot.Mascots.Count; i++)
        {
            var index = i;
            var mascot = AnimatedMascot.Mascots[i];
            var thumbnail = new Image
            {
                Source = AnimatedMascot.GetThumbnail(mascot.Id),
                Width = 40,
                Height = 40,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(thumbnail, BitmapScalingMode.NearestNeighbor);
            var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            content.Children.Add(thumbnail);
            content.Children.Add(new TextBlock
            {
                Text = mascot.Name,
                Width = 46,
                FontSize = 10,
                Foreground = BrushFrom("#FFF4E9"),
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var button = new Button
            {
                Style = style,
                Content = content,
                ToolTip = mascot.Name,
                Tag = index
            };
            AutomationProperties.SetName(button, $"Maskot: {mascot.Name}");
            button.Click += (_, _) => SelectMascot(index);
            MascotItemsPanel.Children.Add(button);
            _mascotButtons.Add(button);
        }
    }

    private void UpdateMascotSelection()
    {
        for (var i = 0; i < _mascotButtons.Count; i++)
        {
            var selected = i == _state.SelectedMascot;
            _mascotButtons[i].Background = BrushFrom(selected ? "#66523F" : "#302B2B");
            _mascotButtons[i].BorderBrush = BrushFrom(selected ? "#F7B580" : "#625855");
            AutomationProperties.SetHelpText(_mascotButtons[i], selected ? "Seçili maskot" : "Maskotu seç");
        }
    }

    private void OpenOptions(bool settings)
    {
        OptionsPopup.IsOpen = false;
        OptionsPopup.HorizontalOffset = _contextMenuPosition.X + 6;
        OptionsPopup.VerticalOffset = _contextMenuPosition.Y + 6;
        if (settings) LoadSettings();
        SettingsView.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        MascotPickerView.Visibility = settings ? Visibility.Collapsed : Visibility.Visible;
        OptionsPopup.IsOpen = true;
        if (!settings)
            UpdateMascotSelection();
    }

    private double CurrentWorkAreaTop()
    {
        var center = PointToScreen(new Point(ActualWidth / 2, ActualHeight / 2));
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(
            (int)Math.Round(center.X), (int)Math.Round(center.Y)));
        var workAreaTop = PointFromScreen(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top));
        return Top + workAreaTop.Y;
    }

    private void LoadSettings()
    {
        _loadingSettings = true;
        try
        {
            FocusMinutesBox.Text = _state.FocusMinutes.ToString(CultureInfo.InvariantCulture);
            ShortBreakMinutesBox.Text = _state.ShortBreakMinutes.ToString(CultureInfo.InvariantCulture);
            LongBreakMinutesBox.Text = _state.LongBreakMinutes.ToString(CultureInfo.InvariantCulture);
            LongBreakEveryBox.Text = _state.LongBreakEvery.ToString(CultureInfo.InvariantCulture);
            foreach (var box in new[] { FocusMinutesBox, ShortBreakMinutesBox, LongBreakMinutesBox, LongBreakEveryBox })
                SetSettingValidity(box, true);
        }
        finally { _loadingSettings = false; }
    }

    private void SettingsTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_ready || _loadingSettings) return;

        var focusValid = TryReadNumber(FocusMinutesBox, 1, 180, out var focus);
        var shortValid = TryReadNumber(ShortBreakMinutesBox, 1, 180, out var shortBreak);
        var longValid = TryReadNumber(LongBreakMinutesBox, 1, 180, out var longBreak);
        var everyValid = TryReadNumber(LongBreakEveryBox, 2, 12, out var every);
        SetSettingValidity(FocusMinutesBox, focusValid);
        SetSettingValidity(ShortBreakMinutesBox, shortValid);
        SetSettingValidity(LongBreakMinutesBox, longValid);
        SetSettingValidity(LongBreakEveryBox, everyValid);
        if (!focusValid || !shortValid || !longValid || !everyValid) return;
        if (focus == _state.FocusMinutes && shortBreak == _state.ShortBreakMinutes &&
            longBreak == _state.LongBreakMinutes && every == _state.LongBreakEvery) return;

        _state.FocusMinutes = focus;
        _state.ShortBreakMinutes = shortBreak;
        _state.LongBreakMinutes = longBreak;
        _state.LongBreakEvery = every;
        _timer.FocusMinutes = focus;
        _timer.ShortBreakMinutes = shortBreak;
        _timer.LongBreakMinutes = longBreak;
        _timer.LongBreakEvery = every;
        ScheduleSave();
        UpdateDisplay();
    }

    private static bool TryReadNumber(TextBox box, int minimum, int maximum, out int value)
    {
        return int.TryParse(box.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value) &&
               value >= minimum && value <= maximum;
    }

    private static void SetSettingValidity(TextBox box, bool valid)
    {
        box.Background = BrushFrom(valid ? "#342F31" : "#573538");
        box.BorderBrush = BrushFrom(valid ? "#655A59" : "#D9877A");
    }

    private void SetMascotVisible(bool visible)
    {
        if (visible == (MascotRow.Height.Value > 0)) return;
        _changingLayout = true;
        try
        {
            if (visible)
            {
                var targetTop = Top - MascotAreaHeight;
                if (IsLoaded)
                    targetTop = Math.Max(targetTop, CurrentWorkAreaTop() + 4);
                Top = targetTop;
                MascotRow.Height = new GridLength(MascotAreaHeight);
                Height = MascotAreaHeight + PanelHeight;
                _state.MascotHidden = false;
            }
            else
            {
                Top += MascotAreaHeight;
                MascotRow.Height = new GridLength(0);
                Height = PanelHeight;
                _state.MascotHidden = true;
            }
        }
        finally { _changingLayout = false; }
        KeepTimerPanelOnScreen();
        RememberPosition();
        SaveState();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu { Style = (Style)FindResource("ModernContextMenuStyle") };
        menu.Resources[typeof(MenuItem)] = FindResource("ModernMenuItemStyle");
        menu.Resources[typeof(Separator)] = FindResource("ModernMenuSeparatorStyle");
        AddAction(menu.Items, "Süre ayarları", () => OpenOptionsAfterMenu(settings: true),
            "M 2,4 L 14,4 M 2,8 L 14,8 M 2,12 L 14,12 M 6,2 L 6,6 M 10,6 L 10,10 M 5,10 L 5,14");
        AddAction(menu.Items, "Maskot seç", () => OpenOptionsAfterMenu(settings: false),
            "M 3,7 C 3,4 5,2 8,2 C 11,2 13,4 13,7 L 13,10 C 13,12 11,14 8,14 C 5,14 3,12 3,10 Z M 5,7 L 5,8 M 11,7 L 11,8 M 6,10 C 7,11 9,11 10,10");
        menu.Items.Add(new Separator());
        AddAction(menu.Items, _state.MascotHidden ? "Mimo’yu göster" : "Mimo’yu gizle",
            () => SetMascotVisible(_state.MascotHidden),
            _state.MascotHidden
                ? "M 1,8 C 3,5 5,3.5 8,3.5 C 11,3.5 13,5 15,8 C 13,11 11,12.5 8,12.5 C 5,12.5 3,11 1,8 Z M 6,8 A 2,2 0 1 1 10,8 A 2,2 0 1 1 6,8"
                : "M 1,8 C 3,5 5,3.5 8,3.5 C 11,3.5 13,5 15,8 C 13,11 11,12.5 8,12.5 C 5,12.5 3,11 1,8 Z M 2,14 L 14,2");
        AddAction(menu.Items, "Çıkış", Close,
            "M 9,2 L 14,2 L 14,14 L 9,14 M 2,8 L 11,8 M 8,5 L 11,8 L 8,11");
        return menu;
    }

    private void OpenOptionsAfterMenu(bool settings) =>
        Dispatcher.BeginInvoke(new Action(() => OpenOptions(settings)), DispatcherPriority.Background);

    private void AddAction(ItemCollection collection, string title, Action action, string iconPath)
    {
        var item = new MenuItem
        {
            Header = title,
            Icon = new WpfPath
            {
                Data = Geometry.Parse(iconPath),
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Stroke = (Brush)FindResource("MenuIconBrush"),
                StrokeThickness = 1.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            }
        };
        item.Click += (_, _) => action();
        collection.Add(item);
    }

    private void ScheduleSave()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void SaveState()
    {
        if (!_ready) return;
        try { _state.Save(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Mimo ayarları kaydedilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _ticker.Stop();
        _saveDebounce.Stop();
        _completionSounds.Dispose();
        RememberPosition();
        SaveState();
    }

    private static Brush BrushFrom(string color) =>
        (Brush)new BrushConverter().ConvertFromString(color)!;
}
