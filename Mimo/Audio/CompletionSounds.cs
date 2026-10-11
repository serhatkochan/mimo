using System;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Reflection;
using Mimo.Core;

namespace Mimo.Audio;

internal sealed class CompletionSounds : IDisposable
{
    private readonly SoundPlayer? _focus = Load("focus-complete.wav");
    private readonly SoundPlayer? _break = Load("break-complete.wav");

    public void Play(SessionKind finished)
    {
        try
        {
            (finished == SessionKind.Focus ? _focus : _break)?.Play();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
        {
            // A missing audio device must not interrupt the timer.
        }
    }

    public void Dispose()
    {
        _focus?.Dispose();
        _break?.Dispose();
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
