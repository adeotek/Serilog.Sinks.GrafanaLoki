# Task 3 Report

## Status: DONE
## Commit: 4b6d09a
## Tests: 141 passed (11 BatchFormatter tests + 130 others), 0 failed

## Changes
- `src/Formatters/BatchFormatter.cs`: passes entry.Metadata through to StreamEntry (null when empty)
- `tests/FormattersTests/BatchFormatterTests.cs`: added 2 tests for metadata in formatted output
