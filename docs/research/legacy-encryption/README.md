# Legacy-encryption research (2026-11-10)

Evidence snapshots for password decryption of legacy .doc/.ppt (and XOR) files.
Research ran against submodule heads npoi 72ff7c7 and b2xtranslator 930217b.

- doc-encryption.md: research report on legacy .doc encryption.
- ppt-encryption.md: research report on legacy .ppt encryption.
- npoi-xor.md: research report on NPOI XOR obfuscation.
- proto/doc/: DocCrypto.cs, NpoiPath.cs, Cfb.cs, Malformed.cs, RebuildCheck.cs, XorCheck.cs, Program.cs (.doc decrypt prototype).
- proto/ppt/: Layout.cs, Crypto.cs, Walk.cs, Program.cs (.ppt decrypt prototype).

The prototypes are throwaway: not built by any project, not production code.
Reports are verbatim and unedited. Research-only scratch paths mentioned in the
reports (downloads\, oracle-out\, out\) are NOT in the repo.

The prototype sources are stored as `.cs.txt` (e.g. `proto/doc/DocCrypto.cs.txt`) so
repo tooling (the TDD guard, globbing) does not treat them as compiled code. Rename to
`.cs` to use them.
