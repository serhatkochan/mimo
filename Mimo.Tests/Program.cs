using Mimo.Core;

var now = new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
var timer = new PomodoroTimer();
var finished = new List<SessionKind>();
timer.SessionFinished += kind => finished.Add(kind);

Assert(timer.Kind == SessionKind.Focus && timer.Remaining == TimeSpan.FromMinutes(25), "default focus");
timer.Start(now);
timer.Tick(now.AddMinutes(10));
Assert(timer.Remaining == TimeSpan.FromMinutes(15), "elapsed time");
timer.Pause(now.AddMinutes(10));
timer.Tick(now.AddHours(3));
Assert(timer.Remaining == TimeSpan.FromMinutes(15), "pause holds time");
timer.Start(now.AddHours(3));
timer.Tick(now.AddHours(3).AddMinutes(15));
Assert(timer.Kind == SessionKind.ShortBreak && !timer.IsRunning, "focus advances to break");
Assert(timer.FinishedFocusRounds == 1 && timer.CurrentRound == 2 &&
       finished.SequenceEqual([SessionKind.Focus]), "focus completion event and round number");
timer.Tick(now.AddHours(4));
Assert(finished.Count == 1, "completion fires only once");

timer.Start(now);
timer.Tick(now.AddMinutes(5));
Assert(timer.Kind == SessionKind.Focus && finished.Count == 2, "break completes");
for (var i = 0; i < 3; i++)
{
    timer.Start(now);
    timer.Tick(now.AddMinutes(25));
    if (i < 2)
    {
        Assert(timer.Kind == SessionKind.ShortBreak, "early breaks short");
        timer.Skip(now);
    }
}
Assert(timer.Kind == SessionKind.LongBreak && timer.FinishedFocusRounds == 4, "fourth focus gives long break");

timer.SelectKind(SessionKind.Focus);
var notificationsBeforeSkip = finished.Count;
timer.Skip(now);
Assert(timer.FinishedFocusRounds == 5 && timer.Kind == SessionKind.ShortBreak,
    "next-phase action advances the round cycle");
Assert(finished.Count == notificationsBeforeSkip, "manual skip has no completion event");
timer.SelectKind(SessionKind.Focus);
timer.FocusMinutes = 40;
Assert(timer.Remaining == TimeSpan.FromMinutes(40), "custom duration updates stopped phase");
timer.Start(now);
timer.Tick(now.AddMinutes(5));
var notificationsBeforeReset = finished.Count;
timer.Reset();
Assert(!timer.IsRunning && timer.Remaining == TimeSpan.FromMinutes(40), "reset current phase");
Assert(finished.Count == notificationsBeforeReset, "reset has no completion event");

var manualCycle = new PomodoroTimer();
for (var round = 1; round <= 8; round++)
{
    Assert(manualCycle.Kind == SessionKind.Focus && manualCycle.CurrentRound ==
        (round - 1) % 4 + 1, $"round label {round}");
    manualCycle.Skip(now);
    Assert(manualCycle.Kind == (round % 4 == 0 ? SessionKind.LongBreak : SessionKind.ShortBreak),
        $"break after round {round}");
    manualCycle.Skip(now);
}
Assert(manualCycle.CurrentRound == 1 && manualCycle.FinishedFocusRounds == 8,
    "cycle restarts after long break");

var continuousCycle = new PomodoroTimer();
continuousCycle.Start(now);
continuousCycle.Skip(now.AddMinutes(2));
Assert(continuousCycle.Kind == SessionKind.ShortBreak && continuousCycle.IsRunning &&
       continuousCycle.Remaining == TimeSpan.FromMinutes(5), "running skip starts short break");
continuousCycle.Tick(now.AddMinutes(3));
Assert(continuousCycle.Remaining == TimeSpan.FromMinutes(4), "new phase counts down immediately");
continuousCycle.Skip(now.AddMinutes(3));
Assert(continuousCycle.Kind == SessionKind.Focus && continuousCycle.IsRunning &&
       continuousCycle.CurrentRound == 2, "running break skip starts next focus");
continuousCycle.Pause(now.AddMinutes(4));
continuousCycle.Skip(now.AddMinutes(4));
Assert(continuousCycle.Kind == SessionKind.ShortBreak && !continuousCycle.IsRunning,
    "paused skip keeps next phase paused");

Console.WriteLine("Pomodoro timer: all checks passed.");

static void Assert(bool condition, string label)
{
    if (!condition) throw new Exception($"Failed: {label}");
}
