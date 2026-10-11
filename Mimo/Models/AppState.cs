using System;
using System.IO;
using System.Text.Json;

namespace Mimo.Models;

public sealed class AppState
{
    private static readonly string[] PreviousMascotOrder =
        ["snail", "cactus", "jellyfish", "ghost", "bee", "capybara", "mushroom", "crab", "seal", "teapot"];
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mimo", "state.json");

    public int SelectedMascot { get; set; }
    public string? SelectedMascotId { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool MascotHidden { get; set; }
    public int FocusMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int LongBreakEvery { get; set; } = 4;

    public static AppState Load()
    {
        try
        {
            if (!File.Exists(StatePath)) return new AppState { SelectedMascotId = "bee" };
            var loaded = JsonSerializer.Deserialize<AppState>(File.ReadAllText(StatePath));
            if (loaded is null) return new AppState { SelectedMascotId = "bee" };
            loaded.SelectedMascot = Math.Clamp(loaded.SelectedMascot, 0, 9);
            if (string.IsNullOrWhiteSpace(loaded.SelectedMascotId))
                loaded.SelectedMascotId = PreviousMascotOrder[loaded.SelectedMascot];
            loaded.FocusMinutes = Math.Clamp(loaded.FocusMinutes, 1, 180);
            loaded.ShortBreakMinutes = Math.Clamp(loaded.ShortBreakMinutes, 1, 180);
            loaded.LongBreakMinutes = Math.Clamp(loaded.LongBreakMinutes, 1, 180);
            loaded.LongBreakEvery = Math.Clamp(loaded.LongBreakEvery, 2, 12);
            return loaded;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppState { SelectedMascotId = "bee" };
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(StatePath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = StatePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, StatePath, true);
    }
}
