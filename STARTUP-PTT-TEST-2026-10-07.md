# PowerSDR 3.0.0.2 startup and RX/TX transition candidate

Base: KE9NS `origin/ke9ns_v3.0.01`, commit `851c3e6` (October 5), plus the
preserved I/Q R2 corrections (`e86cd31`, `ad8b848`). Development branch:
`codex/fix-startup-nr-resume`. Version 3.0.0.2, Release x86, .NET Framework 4.8.

This retains the upstream VFO/slide-rule, meters, scan and memory changes.
The existing local I/Q branch is unchanged. No changes have been pushed.

## Evidence and changes

* The original restart detector counted equal, integer-rounded spectrum sums
  for a few display frames. The allowed interval therefore shrank with FPS.
  All four detector sites now share an elapsed-time watchdog with independent
  RX/view observations. It waits for two seconds of audio progress (at least
  16 blocks), with a bounded 15-second cold-start grace if no audio arrives.
  A further two seconds of unchanged spectrum triggers the existing restart.
  Power and PTT edges reset observations. The counter is not hidden or disabled.
* Native WDSP channel creation occurred in the first real-time callback. The
  original test measured approximately four seconds in that callback here.
  Channels and controls are now prepared before the hardware audio stream starts.
  Double-precision FFTW plans are cached in `wdsp-fftw-x86.wisdom` in the v3
  profile; DttSP's single-precision `wisdom` is not touched. Unicode paths and
  cache import/export are tested. Missing/invalid/unwritable cache is optional.
* The audio RX flag can change before hardware de-keying completes. Original
  DttSP RX mute/switch-ramp processing was not applied to the replacement WDSP
  audio. An explicit transition gate now prevents raw switching input from
  reaching WDSP until hardware return completes. FLEX-3000/5000 then allow a
  conservative 50 ms settling interval, rounded up to an audio block, followed
  by a 5 ms stereo output ramp. This uses the original switch's maximum settling
  budget, not its exact signal-threshold implementation. Other radios have no
  added settling interval. Check CW turnaround on hardware as well as phone.
* This does not close/reopen WDSP, clear learned NR state, feed TX samples or
  synthetic zeros into the NR, alter filters, or migrate the transmitter to WDSP.
  RX2 monitoring during TX is preserved when automatic TX mute is disabled.
* WDSP's nonblocking output-underrun result (-2) already supplies zero samples.
  It now remains a silent WDSP block rather than briefly substituting a DttSP
  block with a different delay/gain/NR. Other errors retain the existing fallback.

## Verification and limits

Release x86 builds successfully (existing project warnings remain). Automated
tests use the real x86 WDSP library and synthetic signals, with hardware/DttSP
exchange stubbed. They cover restart timing/FPS, cold/cached initialization,
stereo ramps at 48/96/192 kHz with 64/256/2048/4096-sample buffers, PTT with a
simulated switching impulse, NR0-4 state retention, RX2/TX isolation, routing,
AGC, EQ, manual/automatic notches, squelch, diversity and I/Q regression.

Measured three-channel preparation: 3981 ms without cache, 223 ms in a fresh
process using that cache on this PC. These are not radio-start benchmarks or
promised timings on other PCs. A synthetic pause alone did not reproduce the
reported hardware symptom. The gate addresses an identified integration gap;
elimination of the audible tail and weak-signal NR recovery delay is NOT yet
confirmed on a FLEX. No DPC trace was captured and DPC latency is not ruled out.

Run `tests/RunIntegrationTests.ps1 -Suite Resume` for the new native tests and
`-Suite State` for startup timing. Run Resume twice to compare cache reuse.

## FLEX-5000 acceptance checklist

1. Back up the v3 profile and shared memory.xml/DXMemory.xml. Close PowerSDR.
2. Extract the entire test ZIP to a new folder, leaving the installed version
   and official drivers in place. Launch that folder's PowerSDR.exe.
3. At 192 kHz, first test NR/NB/ANF off. Key/unkey repeatedly using the headset
   connected to the FLEX itself. Check for tail/click/noise and clipped RX onset.
4. Repeat with each NR, both weak and strong signals, short and long PTT holds.
5. Verify RX2 with TX auto-mute enabled and disabled, RX-S and diversity.
6. Test phone and CW. Report any new turnaround delay or lost first syllable.
7. Close/reopen at 25 and 50 FPS. Power ON several times. Record startup duration
   and whether Auto Restart increments. Compare the first run with a later run.
8. If a symptom remains, send a short audio recording, buffer size, AGC mode,
   NR mode and approximate delay. Compare with v2.8 under identical conditions.

The updated upstream shares memory.xml/DXMemory.xml in the parent FlexRadio
Systems directory; the remaining v3 profile is separate from v2.8. Do not
overwrite databases or copy a cache from another PC into this package.
