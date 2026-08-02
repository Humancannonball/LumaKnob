## What changed

<!-- Describe the focused change and its user impact. -->

## Why

<!-- Explain the problem or root cause. Link the issue where applicable. -->

## Validation

- [ ] `dotnet format LumaKnob.slnx --verify-no-changes`
- [ ] Release build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Simulator smoke-tested
- [ ] Relevant Windows hardware and recovery path tested, or not applicable

## Safety and compatibility

<!-- Describe brightness recovery, multi-monitor, HDR, DPI, and platform implications. -->

- [ ] No elevation, service, driver, telemetry, or nonvolatile monitor write added
- [ ] New third-party code/assets have compatible licenses and notices
- [ ] Commits include a DCO `Signed-off-by` line
