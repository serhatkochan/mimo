#nullable enable

using System;

namespace Mimo.Core;

public enum SessionKind
{
    Focus,
    ShortBreak,
    LongBreak
}

/// <summary>
/// Keeps one Pomodoro phase at a time. The caller supplies the current time so
/// elapsed time remains correct after the computer sleeps or the UI stops ticking.
/// </summary>
public sealed class PomodoroTimer
{
    private int _focusMinutes = 25;
    private int _shortBreakMinutes = 5;
    private int _longBreakMinutes = 15;
    private int _longBreakEvery = 4;
    private TimeSpan _phaseDuration = TimeSpan.FromMinutes(25);
    private DateTimeOffset? _deadline;

    public int FocusMinutes
    {
        get => _focusMinutes;
        set => SetDuration(ref _focusMinutes, value, SessionKind.Focus);
    }

    public int ShortBreakMinutes
    {
        get => _shortBreakMinutes;
        set => SetDuration(ref _shortBreakMinutes, value, SessionKind.ShortBreak);
    }

    public int LongBreakMinutes
    {
        get => _longBreakMinutes;
        set => SetDuration(ref _longBreakMinutes, value, SessionKind.LongBreak);
    }

    public int LongBreakEvery
    {
        get => _longBreakEvery;
        set => _longBreakEvery = Math.Clamp(value, 2, 12);
    }

    public SessionKind Kind { get; private set; } = SessionKind.Focus;

    public bool IsRunning { get; private set; }

    public TimeSpan Remaining { get; private set; } = TimeSpan.FromMinutes(25);

    public int FinishedFocusRounds { get; private set; }

    public int CurrentRound => FinishedFocusRounds % LongBreakEvery + 1;

    public double Progress => Math.Clamp(
        1 - Remaining.TotalSeconds / _phaseDuration.TotalSeconds, 0, 1);

    /// <summary>Reports natural completions and manual skips after the next phase is ready.</summary>
    public event Action<SessionKind>? SessionFinished;

    public void Start(DateTimeOffset now)
    {
        if (IsRunning)
        {
            return;
        }

        _deadline = now + Remaining;
        IsRunning = true;
    }

    public void Pause(DateTimeOffset now)
    {
        if (!IsRunning)
        {
            return;
        }

        Tick(now);
        if (IsRunning)
        {
            IsRunning = false;
            _deadline = null;
        }
    }

    public void Reset()
    {
        IsRunning = false;
        _deadline = null;
        _phaseDuration = DurationFor(Kind);
        Remaining = _phaseDuration;
    }

    public void SelectKind(SessionKind kind)
    {
        var duration = DurationFor(kind);
        IsRunning = false;
        _deadline = null;
        Kind = kind;
        _phaseDuration = duration;
        Remaining = duration;
    }

    public void Skip(DateTimeOffset now)
    {
        // The next-phase action moves through the same round cycle as a finished timer.
        var finished = Kind;
        var keepRunning = IsRunning;
        Advance();
        if (keepRunning)
            Start(now);
        SessionFinished?.Invoke(finished);
    }

    public void Tick(DateTimeOffset now)
    {
        if (!IsRunning || _deadline is null)
        {
            return;
        }

        var remaining = _deadline.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            var finished = Kind;
            Advance();
            if (finished != SessionKind.LongBreak)
                Start(now);
            SessionFinished?.Invoke(finished);
            return;
        }

        Remaining = remaining;
    }

    private void Advance()
    {
        var finished = Kind;
        IsRunning = false;
        _deadline = null;

        if (finished == SessionKind.Focus)
        {
            FinishedFocusRounds++;
            Kind = FinishedFocusRounds % LongBreakEvery == 0
                ? SessionKind.LongBreak
                : SessionKind.ShortBreak;
        }
        else
        {
            Kind = SessionKind.Focus;
        }

        _phaseDuration = DurationFor(Kind);
        Remaining = _phaseDuration;
    }

    private TimeSpan DurationFor(SessionKind kind) => TimeSpan.FromMinutes(kind switch
    {
        SessionKind.Focus => FocusMinutes,
        SessionKind.ShortBreak => ShortBreakMinutes,
        SessionKind.LongBreak => LongBreakMinutes,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    });

    private void SetDuration(ref int field, int value, SessionKind phase)
    {
        var clamped = Math.Clamp(value, 1, 180);
        if (field == clamped)
        {
            return;
        }

        field = clamped;
        if (!IsRunning && Kind == phase)
        {
            _phaseDuration = DurationFor(Kind);
            Remaining = _phaseDuration;
        }
    }
}
