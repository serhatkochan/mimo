"""Generate Mimo's two gentle phase-completion chimes.

Run with ``python scripts/generate_notification_sounds.py`` from the repo root.
Only Python's standard library is required.
"""

from __future__ import annotations

import math
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 24_000
DURATION_SECONDS = 1.04
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "Mimo" / "Assets" / "Sounds"


def soft_note(elapsed: float, frequency: float) -> float:
    if elapsed < 0 or elapsed >= 0.62:
        return 0.0

    # A rounded attack and quick harmonic decay make the notes sound like
    # a small, muted mallet rather than a computer beep.
    attack = min(elapsed / 0.024, 1.0)
    attack = attack * attack * (3.0 - 2.0 * attack)
    tail = min((0.62 - elapsed) / 0.13, 1.0)
    tail = tail * tail * (3.0 - 2.0 * tail)
    envelope = attack * math.exp(-5.0 * elapsed) * tail
    phase = 2.0 * math.pi * frequency * elapsed
    return envelope * (
        0.83 * math.sin(phase)
        + 0.19 * math.sin(2.0 * phase + 0.14)
        + 0.055 * math.sin(3.0 * phase + 0.33)
    )


def write_chime(filename: str, first_hz: float, second_hz: float) -> None:
    pcm = bytearray()
    for index in range(round(SAMPLE_RATE * DURATION_SECONDS)):
        time = index / SAMPLE_RATE
        first = soft_note(time - 0.055, first_hz)
        second = soft_note(time - 0.335, second_hz)
        sample = 0.14 * (first + 0.88 * second)
        pcm.extend(struct.pack("<h", round(max(-1.0, min(1.0, sample)) * 32767)))

    with wave.open(str(OUTPUT_DIR / filename), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm)


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    # Focus ending settles downward into rest; a break ending rises gently.
    write_chime("focus-complete.wav", 392.00, 329.63)  # G4, E4
    write_chime("break-complete.wav", 392.00, 523.25)  # G4, C5


if __name__ == "__main__":
    main()
