# LumaKnob pre-publication code review

Date: 2026-08-02
Scope: all production projects, tests, package dependencies, release scripts,
CI configuration, local logging, lifecycle/recovery paths, and the Windows x64
portable artifact.

## Outcome

The reviewed build is suitable for another supervised Windows hardware test,
but not yet a general-audience stable release. Forty-five automated tests pass,
format/analyzer validation is clean, and NuGet reports no known vulnerable
direct or transitive dependency. Real DDC/CI, WMI, HDR, gamma-ramp, sleep/resume,
and hot-plug behavior still require the physical matrix in `PROJECT.md`.

## Resolved findings

- Split the simulator from the Windows host. The release executable now asks
  Avalonia only for Win32, Skia, and HarfBuzz instead of importing the complete
  cross-platform desktop host.
- Enabled compiled XAML bindings and upgraded Avalonia 12.1.0 to 12.1.1.
- Replaced reflection-based appearance JSON metadata with source-generated
  `System.Text.Json` metadata and rejected preset files larger than 64 KiB.
- Fixed repeated discovery ownership, WMI result checking/object disposal,
  gamma-controller apply/dispose races, color debounce shutdown races, HID
  offset overflow, and raw-input startup waiting without a timeout.
- Made hardware-fault state observable so backend labels cannot claim a healthy
  hardware path after a failed native write.
- Isolated shade failures to the affected monitor. One failed shade no longer
  clears another display's shade or cancels its pending hardware request.
- Paused the animated backplane while the deck is hidden/minimized and avoided
  activating a hidden deck when background brightness input creates a shade.
- Removed dead enum values, properties, commands, a redundant assembly marker,
  redundant project references, packaged debug symbols, and historical design
  captures.
- Sanitized diagnostic messages that previously exposed full user-local paths.
- Expanded tests for repeated discovery, hardware-fault notification, and the
  distinction between a discrete hardware step and below-floor shading.
- Added complete native dependency notices to every portable package.

## Distribution-size decision

Measured Windows x64 executable sizes:

| Mode | Executable | Decision |
| --- | ---: | --- |
| Original self-contained single file | 102,606,706 bytes | Replaced |
| Compressed self-contained, reviewed host | 48,302,821 bytes | Selected |
| Framework-dependent single file | 29,256,902 bytes | Rejected: requires users to install .NET 10 |
| Compressed and trimmed | Publish failed trim analysis | Rejected: unresolved Avalonia and WMI warnings |

Single-file compression preserves easy installation and cuts the executable by
more than half. Compression has a startup decompression cost, so startup is a
clean-Windows smoke-test item. The package script fails if the executable grows
beyond 60 MB. ZIP size will fall much less because ZIP already compresses the
old uncompressed executable.

## Remaining release blockers

- Versioned functional settings and clean-shutdown recovery are not complete.
- Display hot-plug, sleep/resume, unlock, and startup integration are not
  complete.
- Accessibility, high-DPI, multi-GPU, Windows Arm64, and broader physical
  monitor validation remain incomplete.
- The public name, support/security documentation, signing, MSIX/Store identity,
  SBOM, and clean install/uninstall testing remain before a general release.
- The current portable binary is unsigned and should remain clearly labeled as
  an alpha for informed testers.

No trim warning was suppressed and no unsafe packaging shortcut was accepted to
hit a smaller number. Revisit trimming or NativeAOT only when publishing is
warning-free and the resulting Windows build passes the same hardware matrix.
