# WDSP and Direct2D integration into PowerSDR KE9NS v3

## Bases and scope

- Target: `ke9ns/PowerSDR-ke9ns-v3.0.0`, `master` at
  `0b0ed685e9bd9107b2bc03536c2e73be1c696ac4` (based on KE9NS 2.8.0.338).
- Source checkpoint: `0b26fad` from the independently tested KE9NS 2.8.0.312
  WDSP/Direct2D development branch, including physical FLEX-5000 RX2.
- Original source baseline: `9a9ebbb622fc3d32adc7cdb137ee84158fc97b34`.
- Integration uses the source delta, not a replacement of the newer upstream
  files or a merge of the complete obsolete repository history.

Features: independent WDSP receive channels for RX1, RX-S and physical RX2;
NR1/NR2/NR3/NR4, NB1/NB2 without SNB; shared NR/NB parameter pages;
independent RX2 NR selection and RX1/RX2 NR persistence; AGC/pan/gain,
notch and EQ bridge fixes; complex-IQ diversity before RX1 processing;
Direct2D traces/fills/waterfall with GDI+ compatibility and fallback;
display diagnostics, pacing and audio-progress-based startup FPS guard.

This remains x86, with WDSP reporting version 129. It is not a conversion to
x64 or an update to WDSP 2.10. DttSP remains required for TX, legacy spectrum
data and fallback. Direct2D is a hybrid renderer, not a complete GPU rewrite.

## Conflict decisions

- Keep upstream .NET 4.8, NuGet packages, Windows Media Player COM references
  and application version metadata. Add SharpDX alongside them.
- Keep upstream cursor-frequency text and cursor-size settings while queuing
  cursor lines for Direct2D with GDI+ fallback.
- Use the tested elapsed-time FPS scheduler and startup audio-progress guard.
- Select native v145 for VS 18+, otherwise v143, with an explicit
  `PowerSDRPlatformToolset` override available. Keep Release x86 mapping.
- Preserve the legacy Windows-1252 encoding of `display.cs`.

## Build

Use a Windows Visual Studio Developer PowerShell with .NET Framework 4.8,
C++/CLI support, the matching Windows SDK and Windows Media Player COM support.
Follow upstream README instructions to install official KE9NS dependencies
and place the required proprietary runtime DLLs in `bin/Release`.
These installation DLLs and user databases are not added to this branch.

From the repository root:

```powershell
msbuild Console/PowerSDR.csproj /t:Restore /p:RestorePackagesConfig=true /p:Configuration=Release /p:Platform=x86 "/p:SolutionDir=$((Get-Location).Path)\"
msbuild Console/PowerSDR.csproj /t:Build /p:Configuration=Release /p:Platform=x86 "/p:ReferencePath=$((Get-Location).Path)\bin\Release" /m
```

The C# project builds WDSP and PowerMate. Its WDSP target copies the x86
RNNoise, SpecBleach and FFTW dependencies. Retain their licenses and source
notices. The upstream DttSP project is separate from this C# build.
The original x86 `DttSP/libfftw3f-3.lib` and `DttSP/pthreadVC.lib` import
libraries from the previous public KE9NS repository are included to restore
native linking; the new repository did not contain them. These do not replace
the corresponding runtime DLLs or drivers.

To build all three original solution projects plus the added WDSP target:

```powershell
msbuild PowerSDR.sln /t:Build /p:Configuration=Release /p:Platform=x86 "/p:ReferencePath=$((Get-Location).Path)\bin\Release" /m
```

## Validation

### Squelch follow-up

The original squelch controls only reached DttSP. The WDSP bridge now reads
each receiver's enable flag and amplitude/FM thresholds on the audio thread.
Amplitude thresholds retain existing radio calibration and convert DttSP's
summed block power to WDSP's mean-amplitude scale using the receiver DSP
buffer size. FM uses the native FM noise squelch; the two detectors are
mutually exclusive and are reapplied when a channel is recreated.
Run `./tests/RunIntegrationTests.ps1 -Suite Squelch` for independent RX1,
RX-S and RX2 closure, reopening, FM noise/clean signal, bypass and mode/rate
transition checks. No TX processing or display behavior is changed.

Build: the C# project and full solution (including native DttSP) both succeeded
in Release x86 with zero errors on VS 18.9.1; legacy compiler
and analyzer warnings remain. Restore reports security advisories for the
upstream Newtonsoft.Json 10.0.3, SharpZipLib 1.3.1 and System.Net.Http 4.3.3
dependencies. Updating these packages is intentionally a separate change.

Local results on 2026-09-29:

- State: passed all RX1/RX2 NR combinations, legacy profile handling, cold
  startup/stalled audio and 25/31/32/50 FPS target checks.
- Audio: passed RX1/RX-S routing, gain/pan, AGC, EQ, sideband-sensitive notches,
  ANF, diversity, NB1/NB2 and NR1-4, without WDSP fallback.
- Rx2: passed independent controls, NR1-4, NB1/NB2, diversity cancellation,
  TX output isolation, monitor/mute behavior, and 48/96/192 kHz rate changes.
- Graphics: Direct2D initialized and rendered native trace, fill, waterfall
  and lines; snapshot isolation and error fallback/recovery tests passed.
- Speech: a new run was attempted twice, but Windows refused to launch the
  generated test executable with a file-in-use error; the executable was
  subsequently absent. No security settings were changed. The voice test
  needs repeating on another host or after investigating this local issue.
  The same NR3 code passed voice tests on the preceding branch, but that is
  not counted as a completed v3 voice test.

Run the standalone x86 tests against this checkout's built WDSP DLL:

```powershell
./tests/RunIntegrationTests.ps1 -Suite All
```

Suites may also run separately: State, Audio, Rx2, Graphics and Speech.
Graphics requires an interactive Windows graphics session; Speech requires
Windows speech synthesis. Logs are written under `artifacts/tests`.
The DSP harness uses real WDSP with simulated hardware/DttSP, and does not
prove hardware timing or end-to-end compatibility with the FLEX drivers.

Before merging upstream, test on FLEX-5000 with the official matching runtime:
RX1/RX2 and diversity, RX/TX transitions, monitoring/mute, NR persistence,
NR speech quality, NB1/NB2, notches/EQ/AGC, and cold startup at 32-50 FPS.
Also test GDI+ fallback and the upstream cursor-frequency overlay. Back up
the database and use a separate test installation. Earlier successful radio
tests were on the old base, not this newly integrated v3 tree.
