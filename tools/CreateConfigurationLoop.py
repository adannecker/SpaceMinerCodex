"""Create the configuration WAV loop without changing its PCM source."""
from array import array
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys
import wave

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--source", default="Deep Space Configuration.wav")
parser.add_argument("--target", default="Deep-Space-Configuration-Loop.wav")
parser.add_argument("--start", type=int, default=8)
parser.add_argument("--end", type=int, default=188)
parser.add_argument("--report-prefix", default="configuration-loop")
args = parser.parse_args()
if args.start < 0 or args.end - args.start <= 16:
    raise ValueError("Selection must be longer than two eight-second fades")
source = root / "Assets/SpaceMiner/Resources/Audio" / args.source
target = source.with_name(args.target)
if source.resolve() == target.resolve():
    raise ValueError("Source and target must differ")
selection_seconds = args.end - args.start
loop_seconds = selection_seconds - 8
before = hashlib.sha256(source.read_bytes()).hexdigest()
with wave.open(str(source), "rb") as wav:
    channels, width, rate, frames = wav.getnchannels(), wav.getsampwidth(), wav.getframerate(), wav.getnframes()
    if (channels, width, rate, wav.getcomptype()) != (2, 2, 48000, "NONE"):
        raise ValueError("Expected stereo 48 kHz 16-bit PCM WAV")
    if frames < args.end * rate:
        raise ValueError("Source too short for selection")
    wav.setpos(args.start * rate)
    samples = array("h", wav.readframes(selection_seconds * rate))
if sys.byteorder != "little":
    samples.byteswap()

fade_frames = 8 * rate
# Middle, then tail/head blend; final frame meets the original middle's start.
output = samples[fade_frames * channels : (selection_seconds * rate - fade_frames) * channels]
tail_start = (selection_seconds * rate - fade_frames) * channels
for frame in range(fade_frames):
    angle = frame / (fade_frames - 1) * math.pi / 2
    fade_out, fade_in = math.cos(angle), math.sin(angle)
    for channel in range(channels):
        value = round(samples[tail_start + frame * channels + channel] * fade_out
                      + samples[frame * channels + channel] * fade_in)
        if not -32768 <= value <= 32767:
            raise ValueError("Crossfade would clip; output not written")
        output.append(value)

def write_wav(path, data):
    data = array("h", data)
    if sys.byteorder != "little":
        data.byteswap()
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(channels)
        wav.setsampwidth(width)
        wav.setframerate(rate)
        wav.writeframes(data.tobytes())

write_wav(target, output)
assert hashlib.sha256(source.read_bytes()).hexdigest() == before
with wave.open(str(target), "rb") as wav:
    assert wav.getnframes() == loop_seconds * rate
    assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (2, 2, 48000)
logs = root / "Logs"
logs.mkdir(exist_ok=True)
seam = output[-6 * rate * channels:] + output[:6 * rate * channels]
write_wav(logs / (args.report_prefix + "-seams.wav"), seam * 3)
report = {
    "duration_seconds": loop_seconds,
    "selection_seconds": [args.start, args.end],
    "channels": channels,
    "sample_rate": rate,
    "bits": width * 8,
    "peak_absolute_sample": max(abs(value) for value in output),
    "boundary_sample_difference": [output[c] - output[-channels + c] for c in range(channels)],
    "source_sha256": before,
    "loop_sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
    "listening_verified": False,
}
(logs / (args.report_prefix + "-report.json")).write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report, indent=2))
