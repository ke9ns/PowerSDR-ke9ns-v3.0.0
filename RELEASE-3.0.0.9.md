# PowerSDR 3.0.0.9

Based on KE9NS master d1149ae (3.0.0.8). The separate ke9ns-3.0.0.8A branch is not merged here.

- Update the x86 WDSP runtime to 2.11. NR5 and NR6 automatically use their CW models in CWL/CWU and voice models otherwise.
- Feed the optional neural CW decoder with the existing post-DSP RX1 audio tap. Decoder accuracy still requires radio testing.
- Add CAT ZZNE (RX1/RX-S) and ZZNF (physical RX2), with values 0 (off) through 6. See CAT-NR1-NR6.md.
- Preserve the selected NR algorithm when DIGU/DIGL temporarily disable NR, including DIGU-to-DIGL transitions. Returning to a non-digital mode restores the saved algorithm rather than NR1. This temporary memory does not add cross-restart persistence while in digital mode.

## Build and installer

Follow BUILDING-v3.0.0.5.md and MAINTAINER-HANDOFF-3.0.0.5.md (updated for this release). Build Release/x86, with native projects Win32. Restore pinned NuGet packages, including ONNX Runtime 1.20.1. Deploy the newly built wdsp.dll together with PowerSDR.exe and CATStructs.xml. Include the model assets, both FFTW DLLs, RNNoise, SpecBleach, ONNX native x86 files and required notices listed in those instructions.

Retain THIRD-PARTY-NOTICES.md and the component licenses. LICENSE-AUDIT.md records remaining distribution evidence to review before public packaging. Internal QA notes are kept locally rather than in this branch's published tree.

Villas reported successful testing of the preceding WDSP 2.11/CAT build, with no repeat of the previous Defender detection. This does not establish its cause or guarantee future antivirus classification. The additional NR mode-switch fix needs confirmation on the radio before the new MSI is published.
