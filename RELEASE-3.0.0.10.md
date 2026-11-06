# PowerSDR 3.0.0.10

Based on KE9NS master a53c103 (3.0.0.9), including the map download and 3D display updates.

- Update the x86 WDSP runtime to 2.11. NR5 and NR6 automatically use their CW models in CWL/CWU and voice models otherwise.
- Feed the optional neural CW decoder with the existing post-DSP RX1 audio tap. Decoder accuracy still requires radio testing.
- Add CAT ZZNE (RX1/RX-S) and ZZNF (physical RX2), with values 0 (off) through 6. See CAT-NR1-NR6.md.
- Preserve the selected NR algorithm when DIGU/DIGL temporarily disable NR, including DIGU-to-DIGL transitions. Returning to a non-digital mode restores the saved algorithm rather than NR1. This temporary memory does not add cross-restart persistence while in digital mode.

## Build and installer

Follow BUILDING-v3.0.0.5.md and MAINTAINER-HANDOFF-3.0.0.5.md (updated for this release). Build Release/x86, with native projects Win32. Restore pinned NuGet packages, including ONNX Runtime 1.20.1. Deploy the newly built wdsp.dll together with PowerSDR.exe and CATStructs.xml. Include the model assets, both FFTW DLLs, RNNoise, SpecBleach, ONNX native x86 files and required notices listed in those instructions.

Retain THIRD-PARTY-NOTICES.md and the component licenses. LICENSE-AUDIT.md records remaining distribution evidence to review before public packaging. Internal QA notes are kept locally rather than in this branch's published tree.

## Validation and new VOACAP protection

Villas approved the 3.0.0.9 QA package at commit 7090de0, including NR restoration. The merged upstream 3D display changes and the new VOACAP protection in 3.0.0.10 still need radio QA before publication.

The map and VOACAP launch sites now use the existing registered worker/exception boundary. Previously, the current Spot code started raw threads and bypassed it. Failed workers are logged and VOACAP is disabled through a queued UI callback. NOAA's standalone thread entry point is also protected. Missing network adapters and failures fetching NOAA data disable VOACAP, with an English status message and details in shutdown-map.log. Enable VOACAP manually to retry after recovery.

A connected adapter is not proof of Internet access. This is failure handling, not an offline SSN source or a guarantee of immediate detection of a lost WAN connection. Existing request timeouts still apply. No field exception log was available, so the reported crash's exact origin is not established.

Build Release/x86 passed. State, CAT and map tests include synthetic background failures, UI disable notifications, worker cleanup and database checkpoint persistence. They do not emulate the FLEX hardware or external services.

QA must test startup without Internet, loss of Internet while VOACAP is active, re-enable after recovery, and normal close/database persistence. Keep the PR in draft until these new scenarios are confirmed.

Key integration changes use //.10 comments following the maintainer's convention. Upstream WDSP source/model copyright headers are preserved.