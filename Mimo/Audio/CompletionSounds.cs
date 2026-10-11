using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Reflection;
using Mimo.Core;

namespace Mimo.Audio;

internal sealed class CompletionSounds : IDisposable
{
    private readonly Dictionary<string, SoundPlayer?> _players = new()
    {
        ["short-break-start.wav"] = Load("short-break-start.wav"),
        ["short-break-end.wav"] = Load("short-break-end.wav"),
        ["long-break-start.wav"] = Load("long-break-start.wav"),
        ["long-break-end.wav"] = Load("long-break-end.wav"),
        ["control.wav"] = Load("control.wav")
    };

    public void Play(SessionKind finished, SessionKind next)
    {
        PlayFile(finished switch
        {
            SessionKind.ShortBreak => "short-break-end.wav",
            SessionKind.LongBreak => "long-break-end.wav",
            _ => next == SessionKind.LongBreak ? "long-break-start.wav" : "short-break-start.wav"
        });
    }

    public void PlayControl() => PlayFile("control.wav");

    private void PlayFile(string fileName)
    {
        try
        {
            _players[fileName]?.Play();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
        {
            // A missing audio device must not interrupt the timer.
        }
    }

    public void Dispose()
    {
        foreach (var player in _players.Values)
        {
            player?.Stream?.Dispose();
            player?.Dispose();
        }
    }

    private static SoundPlayer? Load(string fileName)
    {
        var name = $"Mimo.Assets.Sounds.{fileName}";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream is null) return null;

        var player = new SoundPlayer(stream);
        try
        {
            player.Load();
            return player;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
        {
            player.Dispose();
            stream.Dispose();
            return null;
        }
    }
}
