using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mimo.Controls;

/// <summary>
/// Plays transparent mascot poses in sync with the display refresh.
/// </summary>
public sealed class AnimatedMascot : Image
{
    private const string AssetRoot = "/Mimo;component/Assets/Mascots/";
    private static readonly Lazy<IReadOnlyList<(string Id, string Name)>> Catalog = new(LoadCatalog);
    private static readonly Dictionary<string, ImageSource> Thumbnails = new(StringComparer.Ordinal);

    private readonly Dictionary<string, BitmapSource[]> _frames = new(StringComparer.Ordinal);
    private readonly ScaleTransform _scale = new();
    private readonly TranslateTransform _translation = new();
    private readonly RotateTransform _rotation = new();
    private SpriteSheet? _sheet;
    private bool _playing;
    private bool _rendering;
    private long _elapsedTicks;
    private long _startedAt;
    private string _mascotId = "bee";
    private string _animationName = "idle";

    public AnimatedMascot()
    {
        Stretch = Stretch.Uniform;
        SnapsToDevicePixels = false;
        UseLayoutRounding = false;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);
        RenderTransformOrigin = new Point(0.5, 0.5);
        var motion = new TransformGroup();
        motion.Children.Add(_scale);
        motion.Children.Add(_rotation);
        motion.Children.Add(_translation);
        RenderTransform = motion;
        Loaded += (_, _) => RefreshPlayback();
        Unloaded += (_, _) => StopRendering();
        IsVisibleChanged += (_, _) => RefreshPlayback();
        SetMascot("bee");
    }

    public static IReadOnlyList<(string Id, string Name)> Mascots => Catalog.Value;

    public string MascotId => _mascotId;

    public string AnimationName => _animationName;

    public bool IsPlaying => _playing;

    public static ImageSource GetThumbnail(string id)
    {
        ValidateMascot(id);
        lock (Thumbnails)
        {
            if (Thumbnails.TryGetValue(id, out var thumbnail))
                return thumbnail;

            var sheet = LoadSheet(id);
            var idle = sheet.Animations["idle"];
            thumbnail = ComposeFrame(sheet, idle, idle.Frames[0]);
            Thumbnails.Add(id, thumbnail);
            return thumbnail;
        }
    }

    public void SetMascot(string id)
    {
        ValidateMascot(id);
        if (_sheet is not null && id == _mascotId)
            return;

        _sheet = LoadSheet(id);
        _mascotId = id;
        _frames.Clear();
        if (!_sheet.Animations.ContainsKey(_animationName))
            _animationName = "idle";
        ShowFirstFrame();
    }

    public void SetAnimation(string name)
    {
        if (_sheet is null || !_sheet.Animations.ContainsKey(name))
            throw new ArgumentException($"Unknown mascot animation: {name}", nameof(name));
        if (name == _animationName)
            return;

        _animationName = name;
        _frames.Clear();
        ShowFirstFrame();
    }

    public void SetPlaying(bool playing)
    {
        if (_playing == playing)
            return;

        if (_playing)
            _elapsedTicks += Stopwatch.GetTimestamp() - _startedAt;
        _playing = playing;
        if (_playing)
            _startedAt = Stopwatch.GetTimestamp();
        else
            UpdateMotion();
        RefreshPlayback();
    }

    private void ShowFirstFrame()
    {
        Source = GetFrames()[0];
        _elapsedTicks = 0;
        _startedAt = Stopwatch.GetTimestamp();
        _translation.X = 0;
        _translation.Y = 0;
        _rotation.Angle = 0;
        _scale.ScaleX = 1;
        _scale.ScaleY = 1;
        RefreshPlayback();
    }

    private void RefreshPlayback()
    {
        var shouldRender = _playing && IsLoaded && IsVisible && _animationName != "idle";
        if (shouldRender == _rendering)
            return;
        if (shouldRender)
        {
            CompositionTarget.Rendering += OnRendering;
            _rendering = true;
        }
        else
            StopRendering();
    }

    private void StopRendering()
    {
        if (!_rendering)
            return;
        CompositionTarget.Rendering -= OnRendering;
        _rendering = false;
    }

    private void OnRendering(object? sender, EventArgs e) => UpdateMotion();

    private void UpdateMotion()
    {
        var profile = _mascotId switch
        {
            "snail" => new Motion(1.4, 0.5, 0.2, 0.014, 3.4),
            "cactus" => new Motion(0.3, 1.7, 0.8, 0.016, 2.7),
            "jellyfish" => new Motion(0.6, 2.3, 0.4, 0.022, 3.1),
            "ghost" => new Motion(0.8, 2.0, 0.5, 0.016, 3.2),
            "bee" => new Motion(0.8, 1.8, 0.8, 0.012, 2.5),
            "capybara" => new Motion(0.2, 0.9, 0.1, 0.018, 4.1),
            "mushroom" => new Motion(0.4, 1.5, 0.6, 0.019, 2.9),
            "crab" => new Motion(2.0, 0.4, 0.4, 0.012, 2.8),
            "seal" => new Motion(0.6, 1.4, 0.5, 0.018, 3.0),
            _ => new Motion(0.4, 0.8, 1.2, 0.013, 3.3)
        };
        var intensity = _animationName switch
        {
            "focus" => 0.8,
            "shortBreak" => 1.0,
            "longBreak" => 0.55,
            _ => 0
        };
        var ticks = _elapsedTicks + (_playing ? Stopwatch.GetTimestamp() - _startedAt : 0);
        var elapsed = ticks / (double)Stopwatch.Frequency;
        var period = GetPeriod(_animationName);
        var inCycle = elapsed % period;
        var frames = GetFrames();
        var frame = frames[Math.Min(frames.Length - 1, (int)(inCycle * 60))];
        if (!ReferenceEquals(Source, frame))
            Source = frame;
        var motionPeriod = profile.PeriodSeconds * (_animationName == "longBreak" ? 1.4 : 1);
        var phase = elapsed * Math.PI * 2 / motionPeriod;
        var breath = (1 - Math.Cos(phase)) / 2 * intensity;
        _translation.X = Math.Sin(phase) * profile.X * intensity;
        _translation.Y = breath * profile.Y;
        _rotation.Angle = Math.Sin(phase) * profile.Rotation * intensity;
        _scale.ScaleX = 1 - breath * profile.Breath * 0.35;
        _scale.ScaleY = 1 + breath * profile.Breath;
    }

    private Animation CurrentAnimation => _sheet!.Animations[_animationName];

    private readonly record struct Motion(double X, double Y, double Rotation,
        double Breath, double PeriodSeconds);

    private readonly record struct Warp(double X, double Y, WarpRegion Region);

    private enum WarpRegion { Top, Bottom, Sides }

    private static double GetPeriod(string animation) => animation switch
    {
        "focus" => 3.0,
        "shortBreak" => 3.4,
        "longBreak" => 4.4,
        _ => 1.0
    };

    private BitmapSource[] GetFrames()
    {
        if (_frames.TryGetValue(_animationName, out var cached))
            return cached;

        var animation = CurrentAnimation;
        var first = ComposeFrame(_sheet!, animation, animation.Frames[0]);
        if (_animationName == "idle")
            return _frames[_animationName] = [first];

        // Deform one pose continuously. Blending separate illustrations makes limbs appear twice.
        var pixels = GetPixels(first);
        var bounds = GetVisibleBounds(pixels, first.PixelWidth, first.PixelHeight);
        var warp = _mascotId switch
        {
            "snail" => new Warp(7, 3, WarpRegion.Top),
            "cactus" => new Warp(5, 4, WarpRegion.Top),
            "jellyfish" => new Warp(10, 5, WarpRegion.Bottom),
            "ghost" => new Warp(8, 4, WarpRegion.Bottom),
            "bee" => new Warp(7, 5, WarpRegion.Top),
            "capybara" => new Warp(4, 2, WarpRegion.Top),
            "mushroom" => new Warp(7, 3, WarpRegion.Top),
            "crab" => new Warp(8, 3, WarpRegion.Sides),
            "seal" => new Warp(6, 4, WarpRegion.Bottom),
            _ => new Warp(5, 7, WarpRegion.Top)
        };
        var intensity = _animationName switch
        {
            "focus" => 0.8,
            "shortBreak" => 1.0,
            _ => 0.55
        };
        var count = (int)Math.Round(GetPeriod(_animationName) * 60);
        var sequence = new BitmapSource[count];
        for (int step = 0; step < count; step++)
            sequence[step] = WarpFrame(pixels, first.PixelWidth, first.PixelHeight,
                bounds, warp, intensity, step * Math.PI * 2 / count);
        return _frames[_animationName] = sequence;
    }

    private static (int Left, int Top, int Right, int Bottom) GetVisibleBounds(byte[] pixels, int width, int height)
    {
        int left = width, top = height, right = 0, bottom = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(y * width + x) * 4 + 3] < 24)
                    continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }
        return (left, top, right, bottom);
    }

    private static BitmapSource WarpFrame(byte[] source, int width, int height,
        (int Left, int Top, int Right, int Bottom) bounds, Warp warp, double intensity, double phase)
    {
        const int size = 128;
        var output = new byte[size * size * 4];
        var columnWave = new double[size];
        for (int x = 0; x < size; x++)
            columnWave[x] = Math.Cos(phase + x * 1.7 / size);

        for (int y = 0; y < size; y++)
        {
            var baseY = (y + 0.5) * height / size - 0.5;
            var normalY = Math.Clamp((baseY - bounds.Top) / Math.Max(1, bounds.Bottom - bounds.Top), 0, 1);
            var rowWave = Math.Sin(phase + normalY * 1.4);
            for (int x = 0; x < size; x++)
            {
                var baseX = (x + 0.5) * width / size - 0.5;
                var normalX = Math.Clamp((baseX - bounds.Left) / Math.Max(1, bounds.Right - bounds.Left), 0, 1);
                var weight = warp.Region switch
                {
                    WarpRegion.Top => Math.Pow(1 - normalY, 2),
                    WarpRegion.Bottom => normalY * normalY,
                    _ => Math.Pow(Math.Abs(normalX * 2 - 1), 2)
                };
                var sampleX = Math.Clamp(baseX - rowWave * warp.X * weight * intensity, 0, width - 1.001);
                var sampleY = Math.Clamp(baseY - columnWave[x] * warp.Y * weight * intensity, 0, height - 1.001);
                var left = (int)sampleX;
                var top = (int)sampleY;
                var right = left + 1;
                var bottom = top + 1;
                var dx = sampleX - left;
                var dy = sampleY - top;
                var topLeft = (top * width + left) * 4;
                var topRight = (top * width + right) * 4;
                var bottomLeft = (bottom * width + left) * 4;
                var bottomRight = (bottom * width + right) * 4;
                var target = (y * size + x) * 4;
                for (int channel = 0; channel < 4; channel++)
                {
                    var upper = source[topLeft + channel] * (1 - dx) + source[topRight + channel] * dx;
                    var lower = source[bottomLeft + channel] * (1 - dx) + source[bottomRight + channel] * dx;
                    output[target + channel] = (byte)Math.Clamp((int)Math.Round(upper * (1 - dy) + lower * dy), 0, 255);
                }
            }
        }
        var result = BitmapSource.Create(size, size, 96, 96,
            PixelFormats.Pbgra32, null, output, size * 4);
        result.Freeze();
        return result;
    }

    private static byte[] GetPixels(BitmapSource bitmap)
    {
        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    private static BitmapSource ComposeFrame(SpriteSheet sheet, Animation animation, int column)
    {
        int stride = checked(sheet.CellWidth * 4);
        var pixels = new byte[checked(stride * sheet.CellHeight)];

        if (sheet.StationaryDeskY is int deskY)
        {
            // Copy the entire reference pose, then replace only the area above the desk.
            CopyRegion(sheet, sheet.Animations["idle"], 0, pixels, 0, 0, sheet.CellHeight);
            Array.Clear(pixels, 0, checked(stride * deskY));
            var offset = animation.FrameOffsets[column];
            CopyRegion(sheet, animation, column, pixels, offset.X, offset.Y, deskY);
        }
        else
        {
            CopyRegion(sheet, animation, column, pixels, 0, 0, sheet.CellHeight);
        }

        var result = BitmapSource.Create(sheet.CellWidth, sheet.CellHeight, 96, 96,
            PixelFormats.Pbgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    private static void CopyRegion(SpriteSheet sheet, Animation animation, int column,
        byte[] destination, int offsetX, int offsetY, int clipBottom)
    {
        int sourceY = animation.SourceY ?? animation.Row * sheet.CellHeight;
        int sourceHeight = animation.SourceHeight ?? sheet.CellHeight;
        int destinationY = animation.DestinationY ?? 0;
        int firstSourceX = Math.Max(0, -offsetX);
        int lastSourceX = Math.Min(sheet.CellWidth, sheet.CellWidth - offsetX);
        int copyBytes = (lastSourceX - firstSourceX) * 4;
        if (copyBytes <= 0)
            return;

        for (int row = 0; row < sourceHeight; row++)
        {
            int fromY = sourceY + row;
            int toY = destinationY + row + offsetY;
            if (fromY < 0 || fromY >= sheet.Height || toY < 0 || toY >= clipBottom)
                continue;

            int sourceOffset = checked((fromY * sheet.Width + column * sheet.CellWidth + firstSourceX) * 4);
            int destinationOffset = checked((toY * sheet.CellWidth + offsetX + firstSourceX) * 4);
            Buffer.BlockCopy(sheet.Pixels, sourceOffset, destination, destinationOffset, copyBytes);
        }
    }

    private static SpriteSheet LoadSheet(string id)
    {
        using var metadataStream = OpenResource(id + ".json");
        using var document = JsonDocument.Parse(metadataStream);
        var root = document.RootElement;
        string imageName = root.GetProperty("image").GetString()!;
        int cellWidth = root.GetProperty("cell_width").GetInt32();
        int cellHeight = root.GetProperty("cell_height").GetInt32();
        int columns = root.GetProperty("columns").GetInt32();
        int? deskY = root.TryGetProperty("stationary_desk_y", out var desk) ? desk.GetInt32() : null;
        var animations = new Dictionary<string, Animation>(StringComparer.Ordinal);

        foreach (var item in root.GetProperty("animations").EnumerateObject())
        {
            var data = item.Value;
            var frames = data.GetProperty("frames").EnumerateArray().Select(value => value.GetInt32()).ToArray();
            var offsets = data.TryGetProperty("frame_offsets", out var rawOffsets)
                ? rawOffsets.EnumerateArray().Select(pair =>
                {
                    var coordinates = pair.EnumerateArray().Select(value => value.GetInt32()).ToArray();
                    return (X: coordinates[0], Y: coordinates[1]);
                }).ToArray()
                : Enumerable.Repeat((X: 0, Y: 0), columns).ToArray();
            animations.Add(item.Name, new Animation(
                data.GetProperty("row").GetInt32(), frames, data.GetProperty("frame_ms").GetInt32(),
                data.TryGetProperty("source_y", out var sourceY) ? sourceY.GetInt32() : null,
                data.TryGetProperty("source_height", out var sourceHeight) ? sourceHeight.GetInt32() : null,
                data.TryGetProperty("destination_y", out var destinationY) ? destinationY.GetInt32() : null,
                offsets));
        }

        using var imageStream = OpenResource(imageName);
        var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var image = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Pbgra32, null, 0);
        int stride = checked(image.PixelWidth * 4);
        var pixels = new byte[checked(stride * image.PixelHeight)];
        image.CopyPixels(pixels, stride, 0);
        return new SpriteSheet(image.PixelWidth, image.PixelHeight, cellWidth, cellHeight,
            columns, deskY, pixels, animations);
    }

    private static IReadOnlyList<(string Id, string Name)> LoadCatalog()
    {
        using var stream = OpenResource("catalog.json");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("mascots").EnumerateArray()
            .Select(item => (Id: item.GetProperty("id").GetString()!,
                Name: item.GetProperty("name").GetString()!))
            .ToArray();
    }

    private static void ValidateMascot(string id)
    {
        if (string.IsNullOrEmpty(id) || !Mascots.Any(item => item.Id == id))
            throw new ArgumentException($"Unknown mascot: {id}", nameof(id));
    }

    private static Stream OpenResource(string name)
    {
        var uri = new Uri(AssetRoot + name, UriKind.Relative);
        return System.Windows.Application.GetResourceStream(uri)?.Stream
            ?? throw new FileNotFoundException($"Embedded mascot resource was not found: {name}");
    }

    private sealed record Animation(int Row, int[] Frames, int FrameMs, int? SourceY,
        int? SourceHeight, int? DestinationY, (int X, int Y)[] FrameOffsets);

    private sealed record SpriteSheet(int Width, int Height, int CellWidth, int CellHeight,
        int Columns, int? StationaryDeskY, byte[] Pixels, Dictionary<string, Animation> Animations);
}
