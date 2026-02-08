# General Quirks

Document cross-cutting quirks that do not fit a specific theme.

## Format

Each entry should include:
- **Issue**: Brief description
- **Context**: When/why this matters
- **Solution/Workaround**: How to handle it

---

*(Add entries below as you encounter them)*

## Entry 1

- **Issue**: Parry/deflect "skipped" debug lines can spam heavily in melee-dense waves.
- **Context**: When debug is enabled, non-player parry traffic generates many repetitive lines per second.
- **Solution/Workaround**: Throttle repeated skip logs by reason and time window instead of logging every event.

## Entry 2

- **Issue**: Multi-line performance summaries per slow-motion session can bury signal in long playtests.
- **Context**: Frequent trigger windows generate repeated baseline/session/worst-frame blocks.
- **Solution/Workaround**: Collapse session-end metrics into a single structured line with duration, avg/worst frame, and drop rate.

## Entry 3

- **Issue**: Trigger behavior is hard to tune from isolated logs because block reasons are distributed across many code paths.
- **Context**: Cooldowns, chance failures, disabled multipliers, and event-level gates (non-player parry/deflect) can all suppress slow-motion.
- **Solution/Workaround**: Record unified telemetry reason codes and review periodic `diag evt=summary` lines to identify the dominant blocker before changing presets.
