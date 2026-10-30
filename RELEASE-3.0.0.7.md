# PowerSDR 3.0.0.7 - maintainer review

Base: upstream master e2b0922, version 3.0.0.6. The KE9NS spot-screen layout
and world precipitation map updates are retained. Branch: powersdr-v3.0.0.7-release.
The website advertised incremental 3.0.0.5 when checked on 2026-10-27; the
repository already contained 3.0.0.6, hence this candidate is 3.0.0.7.

## Changes since upstream 3.0.0.6

- Native WDSP FFT preparation runs on a worker before the audio stream starts.
  A progress dialog shows stage/elapsed time for long preparation, without
  aborting FFTW. The existing wisdom cache is retained.
- The progress dialog appears only if preparation exceeds one second, avoiding
  flashes during fast cached starts and Setup OK restarts. Fast work does not
  incur an unconditional one-second delay.
- Guards prevent reentrant hardware start/stop, close, MOX/Tune and watchdog
  restart during preparation. A pending stop is processed after preparation.
- IARU2/Australia first-run setup reuses BandStack and BandText instead of
  adding duplicate tables. The explicit BandStack-refresh option remains in
  control of resetting entries; disabled refresh preserves user entries.
- Version metadata updated to 3.0.0.7. No new DSP algorithm or native DLL change.

The additional request to capture Console.SaveState from Setup Apply has NOT
been implemented. Existing Apply persistence behavior is unchanged.

## Build / QA / release precautions

Use BUILDING-v3.0.0.5.md and the existing maintainer handoff for Release/x86,
WDSP 2.10 and pinned ONNX Runtime 1.20.1 requirements. Those documents describe
the prior integration; their runtime/build requirements remain applicable.
Villas PY5VB/user feedback approved the previous candidate's functional fixes.
The combined 3.0.0.7 with Darrin's map changes still requires hardware QA.
Automated UI tests cover slow/fast preparation and errors; State/Map suites
cover persistence, watchdog startup, regional tables and shutdown behavior.

The prior QA executable was flagged by Defender as Trojan:Win32/Bearfoos.A!ml.
Static comparison found changes consistent with the intended fixes, but did
NOT establish a false positive. Antivirus investigation remains paused and no
antivirus clearance is claimed for this build. Do not bypass protection.
Resolve this and the remaining LICENSE-AUDIT.md items before public MSI release.
This branch is submitted for source review/build/testing, not automatic merge
or approval of a public binary release.
