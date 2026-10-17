# PowerSDR 3.0.0.5 - WDSP 2.10 / NR5 and NR6

Based on the approved neural CW branch at 8a539bb2d41994bfd435ac2ad1eea833d20a9423.
That branch is retained without changes. No remote publication is implied.

WDSP C/H sources imported from the local Thetis WDSP210 tree at
52b927c87e55e8c3f88b25dad2bf9e22626cea9f. The embedded NNR model sources
and engine retain Warren Pratt's GPL-2.0-or-later notices. Build uses the
existing PowerSDR Win32 project, FFTW and RNNoise/SpecBleach dependencies.
PowerSDR's post-AGC RNNoise scaling fix is explicitly retained.

- NR button cycles OFF and NR1 through NR6, independently for RX1 and RX2.
- NR5 selects NNR model 0 (Standard); NR6 selects model 1 (Premium).
- NR5/NR6 clear and disable BIN independently on RX1/RX2. Leaving NNR
  restores availability in compatible modes, without re-enabling BIN itself.
  Programmatic checkbox changes and stale audio-side BIN requests are guarded.
- Setup > DSP > NNR is immediately after NRs. Seven parameters and two presets
  are available separately for RX1/RX-S and RX2. Settings use Options persistence.
- Pre/Post-AGC position remains shared with the existing DSP > NRs page.
- TX still uses the existing DttSP path; this is not a TX engine migration.
- CW neural remains included. This branch requires its new wdsp.dll, not only
  a replacement PowerSDR.exe. NNR models are embedded in wdsp.dll.

Hardware QA must verify 48/96/192 kHz, RX2/diversity, CW decoding, NR1-6,
PTT transitions, startup and high-FPS rendering. NR6 requires more CPU;
synthetic tests cannot establish real-time performance on the operator's PC.

## Local verification (2026-10-15)

Release x86 build: zero errors, 1335 warnings. State, Audio, Rx2, Resume,
Speech, Map, CwTap, IQ and Squelch integration suites passed. Speech tests
cover NR3/5/6 at 48/96/192 kHz. NnrLayoutTests renders the production panel
with hardware/control stubs and checks 100/150% layout and legacy shortcuts.
No physical FLEX radio or full interactive Setup session was used.

Cold FFTW preparation for three channels took 82 seconds while other tests
were running. A second process using the generated cache took 181 ms.
These are development-host measurements, not performance guarantees.

Manual layout test: compile tests/NnrLayoutTests.cs together with
Console/DSP/WdspNnrSetupPanel.cs, referencing System.Drawing.dll and
System.Windows.Forms.dll; pass an output PNG path to the resulting executable.
