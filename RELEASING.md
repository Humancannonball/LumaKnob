# Releasing LumaKnob

LumaKnob is currently suitable for supervised Windows x64 alpha testing, not a
general-audience stable release. The app already has a portable self-contained
build, recovery hotkey, resident tray process, one brightness control per
monitor, local diagnostics, automated tests, and an MIT license. Production
publication still needs broader physical-device validation, settings/startup,
topology and resume recovery, accessibility review, final naming, and signing.

## First public alpha

1. Choose the final project and publisher names. Create a public GitHub
   repository, add this workspace as its remote, and make the first reviewed
   commit. Never commit signing keys or Partner Center credentials.
2. Enable GitHub Actions and confirm both operating systems pass CI.
3. On real Windows hardware, complete the supervised protocol in `PROJECT.md`,
   including `Ctrl+Alt+Home`, 0% recovery, multi-monitor independence, tray
   lifecycle, DDC-disabled fallback, and exit cleanup.
4. Tag the tested commit with a semantic prerelease version, for example:

   ```shell
   git tag -a v0.1.0-alpha.1 -m "LumaKnob 0.1.0 alpha 1"
   git push origin v0.1.0-alpha.1
   ```

5. The `Draft Windows release` workflow runs all tests, creates the
   compressed self-contained Windows x64 ZIP, enforces the executable size
   ceiling, writes `SHA256SUMS.txt`, and opens a draft GitHub prerelease.
   Download that exact asset and smoke-test it on a clean Windows user account
   before publishing the draft.
6. In the release notes, list tested monitor models, Windows versions, known
   limitations, recovery instructions, and the fact that the alpha is unsigned.

## General-audience Windows release

1. Persist versioned settings atomically and add startup, clean-shutdown,
   display hot-plug, sleep/resume, and shade recovery behavior.
2. Complete the physical device matrix, accessibility/DPI checks, privacy
   statement, security policy, support matrix, SBOM, and uninstall tests.
3. Reserve the Microsoft Store product identity. Prefer MSIX for the primary
   nontechnical installation path; keep the self-contained ZIP as the portable
   expert/tester path.
4. Use Store signing for Store packages. Before publishing a direct-download
   installer, use a trusted production code-signing service and timestamp the
   binaries. A self-signed certificate is only for controlled testing.
5. Publish a stable GitHub release only from the same tested tag and attach
   checksums. Then submit the signed installer manifest to WinGet and test both
   unattended install and uninstall in Windows Sandbox.

Do not tell users to disable SmartScreen, install an untrusted root certificate,
or bypass antivirus. If production signing is not ready, keep the build clearly
marked as a prerelease for informed testers.
