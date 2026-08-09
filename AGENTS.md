# AGENTS.md — Serilog.Sinks.GrafanaLoki

## Build & Test

```bash
dotnet build                    # all targets (netstandard2.0, netstandard2.1, net10.0)
dotnet test --verbosity quiet   # 151 tests, runs on net10.0 only
```

Run a single test or group:
```bash
dotnet test --filter "FullyQualifiedName~Metadata" --verbosity quiet
```

## Project Structure

| Path | Role | Target(s) |
|------|------|-----------|
| `src/Serilog.Sinks.GrafanaLoki.csproj` | Library | `netstandard2.0;netstandard2.1;net10.0` |
| `tests/Serilog.Sinks.GrafanaLoki.Tests.csproj` | Tests (xunit) | `net10.0` |
| `sample/Serilog.Sinks.GrafanaLoki.Sample.csproj` | Sample app | `net10.0` |

**Do not** confuse the stale `test/` directory (orphaned bin/obj from 2023) with the real `tests/` directory.

## Key Dependencies

- Serilog **4.4.0** (latest)
- Serilog.Settings.Configuration **10.0.1** (latest)
- System.Text.Json (built-in)
- Tests: xunit 2.9.3, Microsoft.NET.Test.Sdk 18.8.1
- `JustEat.HttpClientInterception` is a csproj reference but unused in source — do not remove without updating NuGet restore.

## Architecture Notes

### Data Flow
`Emit()` → `LogEventEntry` (Labels + Metadata) → `LogEventsQueue` → `BatchFormatter` → JSON → `GrafanaLokiHttpClient.PostAsync()` → Loki `/loki/api/v1/push`

### Loki API
The sink posts JSON to `/loki/api/v1/push`. The JSON payload is:
```json
{"streams":[{"stream":{"label":"value"},"values":[["<ts_ns>","<line>",{"meta":"data"}]]}]}
```
- Timestamps must be **strings** (nanosecond epoch), not numbers — Loki returns 400 otherwise.
- The optional 3rd elements in `values` are structured metadata (Loki 3.0+).

### Namespace Conventions
- `Serilog.Sinks.GrafanaLoki` — public API (`GrafanaLokiHttpSink`, `LoggerConfigurationGrafanaLokiExtensions`, ctor-types)
- `Serilog.Sinks.GrafanaLoki.Internal` — serialization models, queue, stream grouping
- `Serilog.Sinks.GrafanaLoki.Common` — `IHttpClient`, helpers, utilities
- `Serilog.Sinks.GrafanaLoki.Formatters` — `IBatchFormatter` interface + `BatchFormatter`

### Visibility
`src/Serilog.Sinks.GrafanaLoki.csproj` contains `InternalsVisibleTo` for `Serilog.Sinks.GrafanaLoki.Tests`.
Internal types can be tested directly.

## Testing Patterns

### TestHttpClient
`tests/Infrastructure/TestHttpClient.cs` extends `GrafanaLokiHttpClient` and overrides `PostAsync()` to capture the JSON content and request URI into string properties. Most integration tests create a logger with this client, emit events, call `log.Dispose()` (triggers flush), then `JsonDocument.Parse(client.Content)` to assert.

### Approval Tests
`tests/Infrastructure/ApprovalTests.cs` compares output against `.approved.txt` files stored alongside the test source. The scrubber parameter (typically a regex replacing nanosecond timestamps with `<unixtimestamp>`) normalizes variable content before comparison. When you change the JSON output format, update the corresponding `.approved.txt` file.

### Test Organization
- `tests/FormattersTests/` — BatchFormatter output
- `tests/HttpClientTests/` — HTTP client behavior (auth, post content, exception labels, gzip)
- `tests/IntegrationTests/` — end-to-end Emit → PostAsync pipeline
- `tests/InternalTests/` — queue, stream grouping, dictionary comparer
- `tests/CommonTests/` — helpers, timers, backoff schedule

## Code Style

From `.editorconfig`:
- 4-space indent for `.cs` files
- 2-space indent for `.csproj`, `.json`, `.config`, `.yml`, `.props`
- No trailing whitespace
- No final newline insertion

## Git Workflow

- `main` — release branch, triggers CI (`dotnet_build.yml`, Release config)
- `dev` — development branch, triggers CI (`dotnet_dev_build.yml`, Debug config)
- Feature work on topic branches
- CI targets .NET 7.0 SDK — local development uses .NET 10.0 SDK

## LogEventEntry Caching

`LogEventEntry` is a **struct** with cached byte-size calculation. The cache is invalidated when `Labels.Count` or `Metadata.Count` changes. When adding new mutable properties, update the cache guard and `_size` computation.
