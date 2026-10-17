# Distribution and license audit - candidate 3.0.0.5

Date: 2026-10-17. Status: NOT CLEARED FOR PUBLIC DISTRIBUTION.
This is a technical compliance inventory, not a legal opinion or a guarantee.
No license is changed by this report. Missing evidence is not proof that a
component is prohibited, but must be resolved before claiming compliance.

## Findings

| Component | Evidence inspected | Required action / status |
| --- | --- | --- |
| PowerSDR / WDSP | Root GPL v2 text; source headers commonly GPL-2.0-or-later | Preserve copyright, modifications, license and complete corresponding source/build inputs. Audit exceptions; do not assume every file permits relicensing. |
| WDSP 2.10 NNR models 0/1 | Embedded source headers credit Warren Pratt NR0V, GPL-2.0-or-later | Retain headers and distribute matching source including model arrays. Imported source reference: 52b927c87e55e8c3f88b25dad2bf9e22626cea9f in the local WDSP210 Thetis tree. |
| RNNoise | lib/NR_Algorithms_x86/src/rnnoise/COPYING, BSD 3-clause | Notice now copied by project. Exact embedded weights provenance and prebuilt DLL-to-source mapping still need confirmation. |
| SpecBleach | lib/NR_Algorithms_x86/src/libspecbleach/LICENSE; LGPL-2.1-or-later headers | License now copied. Provide matching source/build instructions and satisfy applicable LGPL replacement/relinking requirements; verify binary provenance. |
| FFTW double and float | Official FFTW FAQ identifies GPL licensing | Identify exact prebuilt versions and matching source, include notices and source. Current local binary directory does not establish complete compliance. |
| DeepFist CW | THIRD-PARTY-NEURAL-CW.txt and upstream Lyra NOTICE identify Brent Crier N9BC, MIT | Preserve MIT notice. Confirm the exact shipped model artifact is covered by that grant; no matching model artifact was found in the inspected upstream tree. |
| ONNX Runtime 1.20.1 | Official package LICENSE and ThirdPartyNotices.txt | Both already copied to output. Keep all bundled third-party notices and verify shipped architecture/version. |
| SharpDX 4.2 | Local binaries | Obtain and archive the exact release license/notices and binary provenance before release. |
| NuGet packages | release-audit/nuget-license-metadata.json, 37 official nuspec records | Metadata is not a complete SBOM or approval. Resolve missing declarations/file licenses and all transitive/native components. |
| Legacy radio/runtime libraries and assets | Historical QA payload | Maintainer must supply origin, versions and redistribution terms. An existing installer is not itself a license grant. |

DeepFist upstream evidence: [pinned NOTICE](https://raw.githubusercontent.com/N8SDR1/Lyra-SDR-cpp/083b0f7025aa09e1dd526df648722950733c1cdf/NOTICE.md).
Local model SHA256: 6D2D4E3D66F9001D15E21A1B38B79150EAE19EAD86A310202900EE69D672B94D.
The independently implemented C# frontend states that no Lyra application code
is used. Do not import GPLv3/AGPL code under an assumed MIT model license.
Other primary evidence: [ONNX license](https://raw.githubusercontent.com/microsoft/onnxruntime/v1.20.1/LICENSE),
[FFTW FAQ](https://www.fftw.org/faq/section1.html).

## Unresolved release gates

1. Review the final payload file-by-file, including maps, images, fonts, datasets,
   skins, voice/model files and executables, not only the new DSP code.
2. Historical QA contains dumpbin.exe, Microsoft.Build.CPPTasks assemblies,
   Visual Studio assemblies, build metadata and other development files.
   Exclude unnecessary development files using a reviewed runtime allowlist.
   Do not simply publish the existing QA ZIP. Do not delete the QA archive.
3. Verify legacy Flex/USB/FireWire libraries, PA19, pthread, LAME, PDFium,
   AutoIt, CEF/Chromium, COM interop and Microsoft runtime redistribution terms.
   Obtain full third-party notices for nested native dependencies.
4. NuGet declares Selenium.WebDriver 4.33.0 Apache-2.0. Review compatibility
   with the actual combined application's GPL terms and all linked components.
   Root GPLv2 text plus some 'or later' headers is not sufficient to conclude
   the entire combined work can be distributed under GPLv3. No relicensing is
   performed here. AutoIt and PdfiumViewer metadata also need further evidence.
5. Establish exact source/version/build mapping for each distributed GPL/LGPL
   binary and preserve notices. For website distribution, provide corresponding
   source and build material with the release, not merely a link to a moving
   upstream branch. Verify the applicable license's precise distribution duties.
6. Resolve exact DeepFist and RNNoise model provenance with their suppliers.
7. Have the maintainer (and qualified counsel where terms are unclear) approve
   the final component/license matrix. Then generate final hashes and test the
   allowlisted installer on a clean machine. Signing and malware scanning are
   useful release checks but do not establish licensing compliance.

## Questions for Darrin / suppliers

- Which legacy DLLs/assets are authorized for redistribution, under which terms,
  and where are their notices and corresponding sources where applicable?
- Can the native FFTW/RNNoise/SpecBleach binaries be mapped to exact source and
  reproducible build commands? What is the source/license of the exact weights?
- Which files are actually required by the installer, versus development-only?
- Can the model supplier confirm the MIT grant covers the DeepFist hash above?

The metadata inventory is intentionally labeled incomplete. No public installer
or public-upload authorization follows from a successful build or QA result.
