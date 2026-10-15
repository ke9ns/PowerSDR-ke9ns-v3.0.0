# PowerSDR 3.0.0.3 - VOACAP shutdown test candidate

Based on the preserved 3.0.0.2 commit 7602c2e. Branch:
`codex/fix-voacap-shutdown-v3.0.0.3`. No WDSP/audio algorithm changes.

## Evidence and scope

The reported shutdown2.log ends after `2a)`, which is emitted AFTER
components.Dispose, but before base.Dispose completes. The final DB.Exit call
was later in ExitConsole, so a visual disposal failure could skip persistence.
The exact original exception/deadlock has not been reproduced locally.

The legacy map worker writes to the PictureBox from a background thread;
shutdown only clears a flag and does not establish that the worker has exited.
The Spotter FormClosing handler also busy-waits on VOATHREAD on the UI thread.
These are concrete lifetime/concurrency defects, not proof of the exact field
failure's stack. The old map file matched the upstream base before these edits.

## Changes

- Register TrackSun, VOACAP and ISS orbit workers before starting them, reject
  duplicate workers and prevent new starts once application closing begins.
- Cancel/defer FormClosing while registered workers drain, using a WinForms
  timer so UI messages (including legacy worker Invoke calls) keep flowing.
  No Join, Thread.Abort or production Application.DoEvents loop is used.
- After 15 seconds without completion, cancel closing and show an English
  warning. The app stays open, with map stopping requested. Wait and close
  again; do not discard live controls or pretend shutdown succeeded.
- Remove the Spotter's busy wait; that form normally hides, rather than disposes.
- Publish the completed tracking bitmap on the UI thread with BeginInvoke;
  ignore queued publications once closing starts. Release Graphics before
  publication. ISS updates its stored points and requests redraw instead of
  drawing through another worker's Graphics object.
- Reset worker/VOACAP state in finally blocks and log full worker exceptions.
- Save a database checkpoint after state/options/child forms have saved, before
  visual disposal. Keep the dataset alive for the existing final save/reset path.
- Log visual disposal exceptions and continue the existing final cleanup/save.
- Correct stale PowerDown log references. Real logs are shutdown1.log,
  shutdown2.log, shutdown3.log and the new timestamped shutdown-map.log in the
  active profile directory. The diagnostic log appends across sessions.

## Tests and limitations

Release x86 build, plus `tests/RunIntegrationTests.ps1 -Suite Map`:
50 worker registration/duplicate/stop/drain cycles; real WinForms UI publication;
suppression of queued images after stop; deferred close while a worker requires
synchronous UI Invoke; exception/finally cleanup; immediate idle close.
These UI tests stub the radio and database. DatabaseCheckpointTests separately
loads the compiled PowerSDR.exe and exercises its actual DB code using only a
synthetic DataSet and new XML file: round trip, retained dataset, write failure
propagation and normal final save/release. No user profile is read or written.

Network services and physical FLEX hardware are not emulated. Test the 15-second
timeout warning and Windows shutdown behavior manually. Existing non-map worker
lifetime issues, full map resource ownership and general UI cross-thread access
elsewhere are outside this focused change. Field confirmation is required.

## Villas/PY5VB acceptance checklist

1. Back up the active v3 profile and shared memories; extract the full test ZIP
   to a separate folder. Keep the official install and drivers unchanged.
2. Confirm version 3.0.0.3. Start without the map, change a harmless setting,
   close and reopen. Confirm persistence.
3. Enable map and VOACAP; repeat the change/close/reopen test several times,
   both while a calculation is active and after the map settles.
4. Repeat with map on and VOACAP off, and with ISS tracking if normally used.
5. Check shutdown2 reaches `2) Done` and `4) Done`, shutdown3 reaches the DB
   completion step, and shutdown-map records a successful database checkpoint.
6. If closing is cancelled or fails, preserve all four logs and any error dialog.
   Do not force-kill the process during a pending calculation unless necessary.
7. Confirm the startup/PTT/NR/RX2 improvements from 3.0.0.2 remain unchanged.

This is a test ZIP, not an MSI or an upstream release. No automatic merge or
publication to Darrin's repository is part of this change.
