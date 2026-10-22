# Third-party components

This integration preserves the licenses of its components; it does not place
third-party code under a new license. See LICENSE-AUDIT.md for unresolved items.

- PowerSDR and WDSP: GPL source headers and LICENSE.txt. The WDSP 2.10 changes
  and embedded NNR model arrays are included in WDSP/. Preserve their authorship
  notices, including Warren Pratt NR0V. See WDSP-2.10-INTEGRATION.md for changes.
- RNNoise: BSD 3-clause, see lib/NR_Algorithms_x86/src/rnnoise/COPYING for all
  copyright holders and terms. Source is in the adjacent directory.
- SpecBleach: Luciano Dato and contributors, LGPL-2.1-or-later source headers;
  full license at lib/NR_Algorithms_x86/src/libspecbleach/LICENSE. The library
  remains a separate DLL. Preserve the user's applicable replacement/debugging
  rights; do not add installer terms forbidding rights granted by the LGPL.
- FFTW: GPL; both double and float builds are used. Exact binary/source mapping
  and full release source must be supplied before distributing those binaries.
- SharpDX 4.2.0: MIT, Alexandre Mutel; full text at lib/SharpDX/LICENSE.txt.
  Retrieved from tag v4.2.0, commit 8e5df9f17b1d328c595a9df5851dbb2537b55621,
  path License.txt (https://github.com/sharpdx/SharpDX/blob/8e5df9f17b1d328c595a9df5851dbb2537b55621/License.txt).
- Neural CW / DeepFist and ONNX Runtime: see THIRD-PARTY-NEURAL-CW.txt and the
  ONNX-Runtime-LICENSE.txt / ONNX-Runtime-ThirdPartyNotices.txt build outputs.
  Model provenance review is documented separately in LICENSE-AUDIT.md.
- NuGet and legacy dependencies retain their own licenses. Run the notice
  collector after restoring packages; it reports missing local license evidence.
  A package's presence in NuGet does not establish compatibility with this app.

For the binary release, retain this index, all license files and component
notices, and provide the complete corresponding source/build materials under
the applicable licenses. This index alone does not fulfill those obligations.
See MAINTAINER-HANDOFF-3.0.0.5.md for the installer review checklist.
