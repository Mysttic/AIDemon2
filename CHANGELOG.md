# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning follows [Semantic Versioning](https://semver.org/).

The `## [X.Y.Z]` heading is read by `release.yml` and becomes the body of the
GitHub release, so write it for people.

## [Unreleased]

## [1.1.0] - 2026-08-01

### Security

- **The SQLCipher database password is no longer hardcoded.** It used to live in
  `Properties/Resources.resx`, which meant one publicly known password shipped in
  every binary and opened every user's database — including the stored API key.
  Each installation now generates its own random key, protected with Windows DPAPI.
  Existing databases are re-keyed on first launch (`PRAGMA rekey`), so history is kept.
- **Generated code is no longer executed without confirmation.** Running a script
  produced by the AI model now requires an explicit confirmation dialog, has a
  30 second timeout, and its whole process tree is killed on cancellation.
- The API key is masked in the settings window instead of being shown in clear text.
- The database moved to `%LOCALAPPDATA%\AIDemon2\`, so it is no longer shared
  between all accounts on the machine.

### Fixed

- **Script output never streamed and large output hung the application.** The
  process output handlers were registered but `BeginOutputReadLine()` was never
  called, and a script writing more than the pipe buffer blocked forever.
- **Closing code fences were left in exported and executed scripts** when the model
  response had no trailing newline, producing syntax errors.
- **Deleted messages reappeared in the favourites list** — the query filtered on
  `Favourite` but not on the soft-delete flag.
- **A changed API key was ignored until restart**; `ResetClient()` did not actually
  reset the client.
- **Connection errors were stored in the database as genuine AI replies**, polluting
  history and exports. They are now logged and surfaced without being persisted.
- Node.js scripts failed when the temporary path contained a space.
- The application could exit before showing a window when the database could not be
  opened; failures are now logged with an actionable message.
- The design-time factory opened the database without a password, so EF Core tooling
  either failed or created an unencrypted database that blocked startup.
- **Every AI reply was rendered as if the user had written it** on a fresh install.
  The author of a message was derived from its programming language, and that language
  comes from the settings — which are empty until the user picks one. Authorship is now
  a stored column; existing history is backfilled by the old rule so conversations look
  unchanged after the update.
- Sending a message without a model selected produced an opaque HTTP error instead of
  saying which setting is missing.
- An empty instruction prompt was still sent to the model as a blank system message.

### Added

- Application logging to `%LOCALAPPDATA%\AIDemon2\logs\` plus global handlers for
  unhandled and unobserved exceptions. There was previously no logging at all.
- Test project (`AIDemon2.Tests`) with 108 tests and a CI workflow running on every
  pull request.

### Changed

- Removed the PostgreSQL provider, ReactiveUI and PropertyChanged.Fody. Property
  change notification is now handled by CommunityToolkit.Mvvm alone; three
  overlapping mechanisms meant every change raised `PropertyChanged` 2-3 times.
- Repositories create a database context per operation instead of sharing a single
  one for the lifetime of the process.
- Soft delete is enforced by a global query filter rather than by remembering to add
  a condition to every query.
- **The red "Delete" button in the message panel is now "Remove from favourites".**
  It never deleted anything: the message stayed in the conversation and in the database.
  The icon, the colour and the confirmation text all promised an irreversible operation
  that did not happen.
- File save dialogs use Avalonia's `StorageProvider` instead of the deprecated
  `SaveFileDialog`.
- The target framework is `net8.0-windows` instead of pinning Windows SDK 10.0.26100,
  which the application never used. Build output dropped from 195 MB to 169 MB.
- **The application is now built as x64 instead of x86.** The releases that actually
  reached users (tags `v1.0.40`–`v1.0.45`) were already published as `win-x64`; only
  the newer MSIX pipeline was pinned to x86, and it has never published a release.
  A 64-bit build addresses more than 4 GB of memory and no longer runs under the WOW64
  emulation layer. Consequences worth knowing:
  - The MSIX package no longer installs on 32-bit Windows or on Windows 10 on ARM64.
  - Scripts the model generates are now executed by 64-bit `powershell`/`cmd`, so they
    see the 64-bit registry and `%ProgramFiles%` without the `(x86)` suffix. WSL-based
    `bash`, previously unreachable from a 32-bit process, now works.
  - Existing data is unaffected: the SQLCipher database file and the DPAPI-protected
    key are both independent of process bitness. Verified by opening a database
    written and migrated by the 32-bit build from a native 64-bit process.
- All compiler warnings are fixed and `TreatWarningsAsErrors` is enabled, so new ones
  cannot accumulate unnoticed. Package versions are managed centrally in
  `Directory.Packages.props`.

[Unreleased]: https://github.com/Mysttic/AIDemon2/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/Mysttic/AIDemon2/releases/tag/v1.1.0
