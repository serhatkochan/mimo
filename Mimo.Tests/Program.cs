using Mimo.Core;

var now = new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
var timer = new PomodoroTimer();
var finished = new List<SessionKind>();
timer.SessionFinished += kind => finished.Add(kind);

Assert(timer.Kind == SessionKind.Focus && timer.Remaining == TimeSpan.FromMinutes(25),
    "default focus duration");

var at = now;
timer.Start(at);
at += TimeSpan.FromMinutes(10);
timer.Tick(at);
Assert(timer.Remaining == TimeSpan.FromMinutes(15), "elapsed focus time");
timer.Pause(at);
at += TimeSpan.FromHours(3);
timer.Tick(at);
Assert(!timer.IsRunning && timer.Remaining == TimeSpan.FromMinutes(15), "pause holds focus time");

timer.Start(at);
at += TimeSpan.FromMinutes(15);
timer.Tick(at);
Assert(timer.Kind == SessionKind.ShortBreak && timer.IsRunning &&
       timer.Remaining == TimeSpan.FromMinutes(5), "completed focus automatically starts short break");
Assert(timer.FinishedFocusRounds == 1 && timer.CurrentRound == 2 &&
       finished.SequenceEqual([SessionKind.Focus]), "focus completion event and round number");
timer.Tick(at);
Assert(finished.Count == 1, "focus completion fires only once");
at += TimeSpan.FromMinutes(1);
timer.Tick(at);
Assert(timer.Remaining == TimeSpan.FromMinutes(4), "automatic break counts down");

at += TimeSpan.FromMinutes(4);
timer.Tick(at);
Assert(timer.Kind == SessionKind.Focus && timer.IsRunning && timer.CurrentRound == 2 &&
       timer.Remaining == TimeSpan.FromMinutes(25), "completed short break automatically starts next focus");
Assert(finished.SequenceEqual([SessionKind.Focus, SessionKind.ShortBreak]),
    "natural phase completions report their own kinds");
at += TimeSpan.FromMinutes(2);
timer.Tick(at);
Assert(timer.Remaining == TimeSpan.FromMinutes(23), "automatic focus counts down");

var naturalCycle = new PomodoroTimer();
var cycleEvents = new List<SessionKind>();
var cycleTransitions = new List<(SessionKind Finished, SessionKind Next)>();
naturalCycle.SessionFinished += kind => cycleEvents.Add(kind);
naturalCycle.SessionFinished += kind => cycleTransitions.Add((kind, naturalCycle.Kind));
var cycleTime = now;
naturalCycle.Start(cycleTime);
for (var round = 1; round <= 4; round++)
{
    cycleTime += TimeSpan.FromMinutes(25);
    naturalCycle.Tick(cycleTime);
    Assert(naturalCycle.Kind == (round == 4 ? SessionKind.LongBreak : SessionKind.ShortBreak) &&
           naturalCycle.IsRunning && naturalCycle.FinishedFocusRounds == round,
        $"round {round} automatically starts its break");

    if (round == 4)
        continue;

    cycleTime += TimeSpan.FromMinutes(5);
    naturalCycle.Tick(cycleTime);
    Assert(naturalCycle.Kind == SessionKind.Focus && naturalCycle.IsRunning &&
           naturalCycle.CurrentRound == round + 1,
        $"short break after round {round} automatically starts next focus");
}

cycleTime += TimeSpan.FromMinutes(15);
naturalCycle.Tick(cycleTime);
Assert(naturalCycle.Kind == SessionKind.Focus && !naturalCycle.IsRunning &&
       naturalCycle.CurrentRound == 1 && naturalCycle.Remaining == TimeSpan.FromMinutes(25),
    "completed long break returns to round one and waits for manual start");
Assert(cycleEvents.SequenceEqual([
        SessionKind.Focus, SessionKind.ShortBreak,
        SessionKind.Focus, SessionKind.ShortBreak,
        SessionKind.Focus, SessionKind.ShortBreak,
        SessionKind.Focus, SessionKind.LongBreak
    ]), "one completion event per naturally finished phase");
Assert(cycleTransitions.SequenceEqual([
    (SessionKind.Focus, SessionKind.ShortBreak),
    (SessionKind.ShortBreak, SessionKind.Focus),
    (SessionKind.Focus, SessionKind.ShortBreak),
    (SessionKind.ShortBreak, SessionKind.Focus),
    (SessionKind.Focus, SessionKind.ShortBreak),
    (SessionKind.ShortBreak, SessionKind.Focus),
    (SessionKind.Focus, SessionKind.LongBreak),
    (SessionKind.LongBreak, SessionKind.Focus)
]), "audio callbacks see the correct destination including the fourth round's long break");
naturalCycle.Tick(cycleTime.AddHours(1));
Assert(!naturalCycle.IsRunning && naturalCycle.Remaining == TimeSpan.FromMinutes(25) &&
       cycleEvents.Count == 8, "completed set does not restart itself");

var customCycle = new PomodoroTimer
{
    FocusMinutes = 2,
    ShortBreakMinutes = 1,
    LongBreakMinutes = 3,
    LongBreakEvery = 3
};
var customTime = now;
customCycle.Start(customTime);
for (var round = 1; round <= 3; round++)
{
    customTime += TimeSpan.FromMinutes(2);
    customCycle.Tick(customTime);
    Assert(customCycle.Kind == (round == 3 ? SessionKind.LongBreak : SessionKind.ShortBreak) &&
           customCycle.IsRunning, $"configured set round {round} starts its break");
    customTime += TimeSpan.FromMinutes(round == 3 ? 3 : 1);
    customCycle.Tick(customTime);
}
Assert(customCycle.Kind == SessionKind.Focus && customCycle.CurrentRound == 1 &&
       !customCycle.IsRunning && customCycle.Remaining == TimeSpan.FromMinutes(2),
    "configured set length also stops at the next first round");

var pausedBreak = new PomodoroTimer();
pausedBreak.Start(now);
pausedBreak.Tick(now.AddMinutes(25));
pausedBreak.Pause(now.AddMinutes(26));
pausedBreak.Tick(now.AddHours(2));
Assert(pausedBreak.Kind == SessionKind.ShortBreak && !pausedBreak.IsRunning &&
       pausedBreak.Remaining == TimeSpan.FromMinutes(4), "paused break does not advance on tick");
pausedBreak.Start(now.AddHours(2));
pausedBreak.Tick(now.AddHours(2).AddMinutes(4));
Assert(pausedBreak.Kind == SessionKind.Focus && pausedBreak.IsRunning,
    "resumed break automatically starts the next focus on completion");

var manualCycle = new PomodoroTimer();
var manualNotifications = new List<SessionKind>();
var manualTransitions = new List<(SessionKind Finished, SessionKind Next)>();
manualCycle.SessionFinished += kind => manualNotifications.Add(kind);
manualCycle.SessionFinished += kind => manualTransitions.Add((kind, manualCycle.Kind));
for (var round = 1; round <= 8; round++)
{
    Assert(manualCycle.Kind == SessionKind.Focus && manualCycle.CurrentRound ==
        (round - 1) % 4 + 1, $"manual round label {round}");
    manualCycle.Skip(now);
    Assert(manualCycle.Kind == (round % 4 == 0 ? SessionKind.LongBreak : SessionKind.ShortBreak) &&
           !manualCycle.IsRunning, $"stopped skip starts the break paused after round {round}");
    manualCycle.Skip(now);
}
Assert(manualCycle.CurrentRound == 1 && manualCycle.FinishedFocusRounds == 8 &&
       !manualCycle.IsRunning && manualNotifications.Count == 16,
    "each paused manual skip reports one transition for audio");
Assert(manualNotifications.SequenceEqual([
    SessionKind.Focus, SessionKind.ShortBreak,
    SessionKind.Focus, SessionKind.ShortBreak,
    SessionKind.Focus, SessionKind.ShortBreak,
    SessionKind.Focus, SessionKind.LongBreak,
    SessionKind.Focus, SessionKind.ShortBreak,
    SessionKind.Focus, SessionKind.ShortBreak,
    SessionKind.Focus, SessionKind.ShortBreak,
    SessionKind.Focus, SessionKind.LongBreak
]), "manual transitions identify short and long break endings separately");
Assert(manualTransitions[6] == (SessionKind.Focus, SessionKind.LongBreak) &&
       manualTransitions[7] == (SessionKind.LongBreak, SessionKind.Focus),
    "manual audio callbacks see long break entry and exit after advancing");

var continuousCycle = new PomodoroTimer();
var continuousNotifications = new List<SessionKind>();
continuousCycle.SessionFinished += kind => continuousNotifications.Add(kind);
continuousCycle.Start(now);
continuousCycle.Skip(now.AddMinutes(2));
Assert(continuousCycle.Kind == SessionKind.ShortBreak && continuousCycle.IsRunning &&
       continuousCycle.Remaining == TimeSpan.FromMinutes(5), "running skip starts short break");
continuousCycle.Tick(now.AddMinutes(3));
Assert(continuousCycle.Remaining == TimeSpan.FromMinutes(4), "running skip counts down immediately");
continuousCycle.Skip(now.AddMinutes(3));
Assert(continuousCycle.Kind == SessionKind.Focus && continuousCycle.IsRunning &&
       continuousCycle.CurrentRound == 2, "running break skip starts next focus");
continuousCycle.Pause(now.AddMinutes(4));
continuousCycle.Skip(now.AddMinutes(4));
Assert(continuousCycle.Kind == SessionKind.ShortBreak && !continuousCycle.IsRunning,
    "paused skip keeps next phase paused");
Assert(continuousNotifications.SequenceEqual([
    SessionKind.Focus, SessionKind.ShortBreak, SessionKind.Focus
]), "running and paused skips report each transition once");
continuousCycle.Tick(now.AddHours(1));
Assert(continuousNotifications.Count == 3, "paused manual transition does not notify again on tick");

var settingsTimer = new PomodoroTimer();
settingsTimer.FocusMinutes = 40;
Assert(settingsTimer.Remaining == TimeSpan.FromMinutes(40),
    "custom duration updates stopped phase");
settingsTimer.Start(now);
settingsTimer.Tick(now.AddMinutes(5));
var resetNotifications = 0;
settingsTimer.SessionFinished += _ => resetNotifications++;
settingsTimer.Reset();
Assert(!settingsTimer.IsRunning && settingsTimer.Remaining == TimeSpan.FromMinutes(40),
    "reset stops and restores current phase");
Assert(resetNotifications == 0, "reset has no completion event");

Console.WriteLine("Pomodoro timer: all checks passed.");

static void Assert(bool condition, string label)
{
    if (!condition) throw new Exception($"Failed: {label}");
}
