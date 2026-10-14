# PowerSDR 3.0.0.5 - experimental neural CW RX

Base: approved integrated 3.0.0.4 (`922badc`). No band-stack experiment included.

The CWX window now contains an optional RX1 neural decoder panel: Enable,
Gate (2-20, default 4), status/tone/keying indicator, received text and Clear RX.
It follows the Thetis CWX decoder controls and colors. The panel is appended
below KE9NS's existing TX editor, not at the same pixel position as Thetis,
to preserve existing transmit controls. Expand CWX to see it. Starts disabled.

The independent C# frontend and the exact DeepFist model/JSON were ported from
our Thetis release tree. No Lyra application source was imported. MIT model and
ONNX notices ship in the output. Model SHA256:
6D2D4E3D66F9001D15E21A1B38B79150EAE19EAD86A310202900EE69D672B94D.

ONNX Runtime CPU 1.20.1 is packaged specifically for Windows x86, not the x64
DLL used by Thetis. Restore Microsoft.ML.OnnxRuntime and its Managed package
at 1.20.1 (Console/packages.config); the project copies model, runtime and notices.

Audio is captured only from active RX1 CWL/CWU after successful WDSP processing,
before AF gain/pan and RX-S mixing. It does not capture the transmit pair.
The bounded queue drops excess blocks, never waits for an inference or lock in
the radio callback, and runs inference on a lower-priority worker. Closing CWX
or disabling the decoder detaches capture before disposing resources. Callback
failures are isolated from radio audio. Existing radio DSP/TX algorithms are unchanged.

The model needs a six-second audio window. This is a receive text decoder, not
instantaneous character detection. Changing audio sample rate while enabled
requires toggling Enable off/on; the status reports this. RX2 decoding is not
implemented. Decoder activation is deliberately not restored at startup.

Validation uses the actual compiled application decoder and actual x86 ONNX
inference, fed synthetic CW. Separate CwTap tests exercise the production WDSP
bridge with hardware stubs: RX1 pre-AF capture, TX/USB excluded and callback
exception isolation. Layout tests render the production panel with radio/decoder
stubs; they do not validate the complete CWX window on the user's display.
State, Map and Audio regression suites pass. Hardware QA remains required.

QA: back up the profile, extract the complete ZIP to a new directory, open CWX
and expand it, select RX1 CWL/CWU, enable CW neural RX and wait for the buffer.
Test clean/weak CW, gate changes, Clear RX, enable/disable, CWX close/reopen,
PTT return, other modes, AF mute and sample-rate changes (toggle decoder afterward).
Verify existing CWX transmit functions and normal VOACAP shutdown still work.
Do not substitute only PowerSDR.exe: the model and ONNX runtime are mandatory.
