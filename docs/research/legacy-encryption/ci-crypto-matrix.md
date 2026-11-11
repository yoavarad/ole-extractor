# Spike 371 (c): crypto on the CI OS matrix

Question: can `ci.yml`'s matrix (ubuntu-latest, windows-latest) run `System.Security.Cryptography` AES-CBC / SHA-512 / HMAC-SHA512 and NPOI main's Standard decryptor, with no platform-gated cipher and no SkiaSharp native load?

Method: throwaway xunit tests in `ExtractorOLE.Tests` on branch `test/371-crypto-matrix-spike` (not merged), run by CI via `workflow_dispatch` (run 42971508409) and locally on Windows 11.

## Observed (run, not assumed)

| Check | ubuntu-latest | windows-latest |
|---|---|---|
| `Aes` CBC (PaddingMode.None) round trip, `SHA512.HashData`, `HMACSHA512.HashData` | pass | pass |
| NPOI main `EncryptionInfo(fs, EncryptionMode.Standard)` -> `Encryptor` write -> `Decryptor.VerifyPassword` (right and wrong password) -> `GetDataStream` byte-for-byte round trip | pass | pass |
| Full CI test job (830 tests, 0 skipped) | pass | pass |

No SkiaSharp type is touched on these paths.

## Finding that matters for the plan

NPOI **main** has no Agile support: `new EncryptionInfo(fs, EncryptionMode.Agile)` throws `EncryptedDocumentException: Not found type NPOI.POIFS.Crypt.Agile.AgileEncryptionInfoBuilder` (the builder lives in NPOI's OOXML project, which this repo does not reference). This matches the already-recorded decision to port Agile decryption rather than reference NPOI ooxml; it does not change an ADR-012 decision. The ported code needs only the primitives proven above.

## Check for downstream tasks

None needed beyond keeping the Agile/Standard decrypt paths on `System.Security.Cryptography` + NPOI main `CryptoFunctions`; keep one round-trip test per cipher path so both matrix OSes keep proving it.
