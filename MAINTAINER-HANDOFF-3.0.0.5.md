# Darrin: source review, build and MSI handoff

Candidate 3.0.0.5, branch powersdr-v3.0.0.5-release, based on upstream 3.0.0.4.
This is a review branch, not a request to publish an unreviewed MSI.
No master change, forced push or automatic merge is intended.

## What changed

- WDSP 2.10, NR5/NR6 and embedded NNR models; separate DSP > NNR settings.
- NR5/NR6 disable BIN for the affected receiver to avoid one-sided audio.
- Optional neural CW decoder integrated into CWX.
- Existing upstream fixes retained. See the feature-specific documents.
- License texts copied with the build, a third-party index, notice collector,
  package metadata inventory and installer payload check.

## Build and review

Follow BUILDING-v3.0.0.5.md. Restore the pinned packages and build Release/x86.
Legacy runtime references are still required; the repository is not claimed to
replace all external hardware SDK/runtime distributions.
Test with the radio before publishing; software-only checks do not certify RF
behavior. QA feedback on the earlier experimental build is not a test of a new MSI.

## Prepare the installer

1. Use a new staging folder and your reviewed runtime file list. Do NOT copy
   the entire historical QA folder or bin/Release indiscriminately.
2. Run `tools/Collect-ReleaseNotices.ps1 -Destination <new-notices-folder>`.
   Merge its notices into staging, preserving paths. Review its JSON report:
   a copied notice is not proof that the full component is covered.
3. Include build-generated ONNX notices and all existing third-party notices
   required by your legacy distribution. Preserve original copyright headers.
4. Resolve LICENSE-AUDIT.md, including legacy grants, exact native sources,
   model provenance and Apache-2.0/GPL compatibility. The latter is a real
   compatibility question, not cured by copying a license text; do not silently
   relicense the whole project. See https://www.apache.org/licenses/GPL-compatibility.html.
5. Run `tools/Test-InstallerPayload.ps1 -PayloadPath <staging> -ReportPath <new-report.json>`.
   It rejects known development files/missing core notices and records SHA256s.
   Passing this check is NOT a complete compliance approval or dependency check.
6. Archive the exact source commit and complete corresponding dependency sources,
   build scripts and notices with the binary release, satisfying each applicable
   license. A moving GitHub branch or generic project homepage is insufficient
   evidence that recipients can obtain the exact corresponding sources.
7. Update your MSI version/file list, preserve LGPL replacement rights and test
   a fresh install on a machine without Visual Studio. Then sign and publish only
   after the remaining release checks are resolved. No installer is generated here.

Please confirm the legacy redistribution/source records already held by KE9NS
and any missing model provenance with the respective suppliers. No third-party
license is changed and no unsupported legal guarantee is made by this handoff.
