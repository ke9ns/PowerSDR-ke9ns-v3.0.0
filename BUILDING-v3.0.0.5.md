# Building PowerSDR 3.0.0.5

This candidate includes upstream master 1e6abdb (3.0.0.4), neural CW,
WDSP 2.10 with NR5/NR6, the nested DSP > NNR page and the NR5/NR6 BIN lock.
The earlier private QA label 3.0.0.6 is not the public release sequence.
The experimental and CW-only branches are preserved separately.

## Prerequisites

- Visual Studio 2022: .NET desktop development, Desktop development with C++,
  C++/CLI support, MSVC v143, Windows SDK and .NET Framework 4.8 targeting pack.
- Open PowerSDR.sln, select Release / x86. Native projects use Win32.
- Restore Console/packages.config using NuGet package restore. Keep the pinned
  versions, particularly Microsoft.ML.OnnxRuntime and its Managed package 1.20.1.
- Legacy reference/runtime assemblies are still required in bin/Release.
  Obtain them from the maintainer's authorized development/runtime distribution.
  Their presence on disk does not establish public redistribution rights.
  A completely self-contained clean-clone build is not claimed.

From a VS Developer PowerShell in the repository root:

```powershell
msbuild Console\PowerSDR.csproj /t:Restore /p:RestorePackagesConfig=true /p:Configuration=Release /p:Platform=x86
msbuild PowerSDR.sln /t:Build /p:Configuration=Release /p:Platform=x86 /p:PowerSDRPlatformToolset=v143 "/p:ReferencePath=$PWD\bin\Release" /m
```

The local validation host uses Visual Studio 18, not VS2022. The project selects
v143 for VS17 and supports an explicit PowerSDRPlatformToolset override.
The build automatically builds the WDSP Win32 project and copies its runtime.
Keep PowerSDR 32-bit: the existing hardware interfaces are not migrated to x64.

## New runtime requirements

- wdsp.dll built from this source tree; NR5/NR6 models are embedded in it.
- rnnoise.dll, specbleach.dll, libfftw3-3.dll AND libfftw3f-3.dll from the
  validated x86 dependencies. Do not mix old DLLs with the new executable.
- Microsoft.ML.OnnxRuntime.dll, native win-x86 onnxruntime.dll and
  onnxruntime_providers_shared.dll from NuGet 1.20.1.
- models/deepfist.onnx and its JSON configuration for optional neural CW.
- Existing SharpDX 4.2 and legacy radio/application dependencies remain required.
- Keep THIRD-PARTY-NEURAL-CW.txt, ONNX-Runtime-LICENSE.txt,
  ONNX-Runtime-ThirdPartyNotices.txt and the licenses directory.

The application still uses DttSP for parts of its processing, including TX;
this is not a complete replacement of every DttSP path with WDSP.

## Validation and installer handoff

Run tests/RunIntegrationTests.ps1 with the appropriate Suite (State, Map, Audio,
Rx2, Resume, Speech, Squelch, IQ, CwTap). Software tests do not replace radio QA.
Verify CW mode, NR1-6, RX2, PTT release, restart/profile persistence and BIN lock
on a FLEX radio before publishing. NR5/6 intentionally disable BIN.

Do not build the installer by indiscriminately copying bin/Release or the old
QA ZIP: they contain development tools and legacy assets requiring review.
Resolve every release gate in LICENSE-AUDIT.md, create a reviewed runtime
allowlist, and test installation on a clean Windows machine without Visual Studio.
Use authorized Microsoft redistributable installers where needed, not arbitrary
developer DLLs. The maintainer must update installer version/payload explicitly.
Publish the exact corresponding source, build instructions and required notices
alongside the binaries; record hashes of the final reviewed installer payload.
