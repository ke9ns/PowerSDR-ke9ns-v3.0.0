# PowerSDR 3.0.0.4 - integrated VOACAP shutdown fix

Base: Darrin's `ke9ns_.3` at `2336ea4`, including `6d514d5`.
Includes the CAT RX2 Spoof/SO2R, Spotter and incremental updater changes from
that branch, plus the VOACAP shutdown fix field-tested by Villas on our earlier
local 3.0.0.3 build (`743766e`).

The shutdown fix coordinates map worker termination without blocking the UI,
publishes map images on the UI thread, writes a database checkpoint before
visual disposal and adds shutdown-map.log diagnostics.

The unsuccessful last-VFO/band-stack experiment (`33cd297`) is NOT included.
The locked band-stack startup behavior remains unchanged and is a separate issue.
This release supersedes the discarded local 3.0.0.4 Last-VFO test ZIP; they are
different builds despite sharing a version number. Use this branch/commit to
identify the integrated release, not that older test ZIP.

Validation: Release x86 build and State/Map software regression suites.
The combined build still needs hardware QA; the original VOACAP fix was
field-tested, but software tests do not validate CAT/SO2R on a physical radio.

Installer handoff: the Advanced Installer project and cache are preserved
exactly as supplied by Darrin. Before distributing an installer, regenerate it
from this build and update its product display name, package filename and MSI
version/upgrade metadata using Darrin's installer workflow. The inherited AIP
still identifies the 3.0.0.3 package; no new installer is claimed here.
