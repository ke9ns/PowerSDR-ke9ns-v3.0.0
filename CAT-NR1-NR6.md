# CAT selection of noise reduction

Use the existing configured CAT serial/TCP connection. Commands end in `;`.
The extended commands match Thetis naming and do not alter legacy `NR`/`ZZNR`.

| Selection | RX1 (and RX-S) | Physical RX2 |
| --- | --- | --- |
| OFF | `ZZNE0;` | `ZZNF0;` |
| NR1 | `ZZNE1;` | `ZZNF1;` |
| NR2 | `ZZNE2;` | `ZZNF2;` |
| NR3 | `ZZNE3;` | `ZZNF3;` |
| NR4 | `ZZNE4;` | `ZZNF4;` |
| NR5 | `ZZNE5;` | `ZZNF5;` |
| NR6 | `ZZNE6;` | `ZZNF6;` |

Query `ZZNE;` or `ZZNF;`: response e.g. `ZZNE5;` / `ZZNF6;`.
Successful writes return no response, following the existing CAT convention;
query afterwards to confirm. Invalid values/lengths return `?;` in normal mode.
Verbose CAT error reporting follows the existing parser setting.

Selection follows the GUI path: updates button, WDSP, NR1 fallback and BIN lock.
RX2 selection does not turn on or provision missing RX2 hardware. In CWL/CWU,
NR5/6 use the CW models; in voice modes they use voice models. The RX1 neural CW
decoder must still be enabled separately in CWX. No TX audio processing changes.
Mode persistence remains the existing profile save behavior.

Deploy BOTH matching PowerSDR.exe and CATStructs.xml, plus the WDSP 2.11 DLL.
The full QA package includes them. Do not use an older CATStructs.xml.
Parser tests use a synthetic backend; physical serial/TCP and radio behavior
still require QA. Use the normal radio safety precautions; this command only
selects RX noise reduction, never starts transmission.
