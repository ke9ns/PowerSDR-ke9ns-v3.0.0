# WDSP 2.11 / mode-aware CW noise reduction - experimental x86 branch

Based on KE9NS 3.0.0.8 (d1149ae). The Windows port is derived from the local
Thetis WDSP 2.11 snapshot 62f15413. The model arrays 0-3 match the TAPR
OpenHPSDR-wdsp release tree at e69de50 (ignoring line endings). Native code
and embedded weights retain upstream notices. The application remains x86.
PowerSDR keeps its NR3 post-AGC normalization and existing DttSP TX path.

NR5 selects standard model 0 for non-CW modes, 2 for CWL/CWU. NR6 selects
premium model 1 or 3 respectively. Crossing the CW boundary updates an active
receiver without cycling the NR button. BIN protection and RX2 independence
are unchanged. The user chooses whether to enable NR; enabling the CW decoder
does not silently enable an NR or change the other receiver.

In CWX enable neural decoding for RX1 and select NR5 or NR6 in CWL/CWU.
The existing RX1 post-WDSP audio tap feeds the optional DeepFist decoder before
speaker AF/pan/mixing. No second pass, ONNX dependency change or new TX capture.
This is preprocessing, not decoder retraining. Better recognition is not
guaranteed: compare OFF/NR5/NR6 on the same weak, keyed CW recording and count
wrong/missing characters. Check SSB and NR3 separately for regressions.

Build the solution Release/x86 using the existing maintainer build guide;
deploy the matching PowerSDR.exe and Win32 wdsp.dll together. The application
checks for native version 211. Earlier binary release/QA notes describe 2.10.
This branch is unrelated to and does not clear the earlier Defender detection.

Local validation: Release/x86 compiled; CwTap exercised native slots 2/0/1/3
on CWU/USB/CWL transitions, captured post-DSP RX1 audio with AF muted, and
excluded TX/non-CW. This checks routing, not actual decoded text accuracy.
The broader Audio suite was not completed: its generated test executable was
unavailable after a sharing error. FLEX hardware and voice regression QA remain
outstanding. No installer, antivirus clearance or upstream release is implied.
