# Responsive WDSP FFT preparation

The first Start can take over a minute when wdsp-fftw-x86.wisdom is absent.
Previously PrepareForAudio ran synchronously on the console UI thread, making
the application appear hung. This patch retains FFTW planning quality and the
existing cache, but performs preparation on a dedicated worker before Audio.Start.

A modal English dialog shows the stage, elapsed time and marquee animation.
The worker starts before the dialog is displayed. A bounded wait of at most
1000 ms suppresses the window for fast cached restarts (including Setup OK).
If preparation takes longer, the modal message loop remains responsive until
completion. This does not skip preparation or assume that a cache is valid.
There is no fabricated percentage or fixed timeout. Native planning is not
aborted. The dialog closes automatically when preparation finishes. Closing
the console, MOX/Tune and reentrant hardware start/stop are guarded during this
interval. A pending stop is handled after preparation; watchdog observation is
suppressed during planning and reset before audio starts.

The existing WDSP/DttSP fallback policy is unchanged. An unexpected exception
escaping preparation is displayed and startup stops. Existing per-channel
fallback diagnostics remain in Debug Output; this is not a missing-DLL fix.

No native DLL or model changes; no version increment or remote upload in this
patch. Build the updated Release/x86 executable using the existing 3.0.0.5
runtime dependencies. Do not combine it with an older WDSP runtime.

Validation: WdspPreparationDialogTests checks UI timer responsiveness, separate
worker, premature-close prevention, propagated failure and immediate completion.
Run it with .NET Framework csc, System.Drawing and System.Windows.Forms references.
RunIntegrationTests.ps1 -Suite Resume tests native wisdom import/export and
NR0-6 transitions. Cold planning in the isolated test took 76.4 seconds for three
receivers; time varies by CPU and configuration. No user profile was deleted.

Radio QA: close PowerSDR, back up the profile's WDSP wisdom, and temporarily
rename that file (do not remove the legacy DttSP wisdom). Start the candidate:
the preparation dialog should remain responsive and audio should begin after it
closes. Verify the new cache exists, close/reopen and compare the cached start.
Check normal receive, RX2, PTT release and start/stop. Real FLEX hardware QA is
still required; automated tests do not confirm radio behavior.
