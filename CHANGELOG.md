# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning follows [Semantic Versioning](https://semver.org/).

The `## [X.Y.Z]` heading is read by `release.yml` and becomes the body of the
GitHub release, so write it for people.

## [Unreleased]

## [2.1.1] - 2026-08-01

### Fixed

- **Every message showed its timestamp twice.** The second line is meant to appear only
  on a message that was edited, and it shows when that happened — but a message was
  counted as edited from the moment it was created. Both dates were stamped with separate
  reads of the clock, and `DateTime.UtcNow` resolves to 100 ns on Windows, so the second
  read could land a tick after the first. That was enough for "modified after creation"
  to be true; the difference itself vanished in the `HH:mm:ss` format, leaving what looked
  like the same time printed twice. The clock is now read once per message.

## [2.1.0] - 2026-08-01

### Added

- **Releases now ship a portable ZIP, and that is the artifact to use.** Unpack it and run
  `AIDemon2.exe` — no installer, no .NET runtime needed, nothing written outside
  `%LOCALAPPDATA%\AIDemon2\`. About 42 MB, self-contained.

### Fixed

- **Previous releases could not be installed at all.** The MSIX shipped unsigned, and Windows
  refuses a package with no trust chain outright — not a warning to click through, but a flat
  refusal. `CONTRIBUTING.md` compounded this by claiming the unsigned package was "installable
  via the bundled `Add-AppDevPackage.ps1`", which was wrong on both counts: that script is not
  in the release, and it would not have helped. The MSIX is still published for the day a
  certificate is available, and both files now say plainly which one to use.

## [2.0.1] - 2026-08-01

### Fixed

- **The interface mixed two languages.** Button labels were English, but everything that
  appears after an action was still Polish: the confirmation for removing a message from
  favourites, the export format chooser and its "Anuluj" button, the save-file dialog and
  its filter names, and the error messages shown in the conversation — a missing API key,
  no selected model, a failed connection. Startup failures (database, SQLCipher re-keying)
  and the reasons a language is unavailable on the current system were Polish too, as were
  the log entries. All of it is English now; only source comments stay Polish.
- A long model identifier ran into the timestamp in the message bubble
  (`openai/gpt-oss-20b:free14:00:00`). The name is now ellipsised and given a gap.

## [2.0.0] - 2026-08-01

> **Upgrading from 1.0.x requires one manual step.** The application no longer uses io.net;
> it talks to [OpenRouter](https://openrouter.ai). Create an account there, generate an API
> key and paste it into the settings — the old io.net key will not work. **Pick the model
> again as well**: io.net identifiers are not OpenRouter identifiers, so the previously saved
> one will not resolve. Your conversation history is preserved.

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

### Added

- Application logging to `%LOCALAPPDATA%\AIDemon2\logs\` plus global handlers for
  unhandled and unobserved exceptions. There was previously no logging at all.
- Test project (`AIDemon2.Tests`) with 159 tests and a CI workflow running on every
  pull request.

### Changed

- **The application now talks to OpenRouter instead of io.net.** One API in front of models
  from many providers, and the model list is fetched from it live instead of being hardcoded
  in application resources — so a model added or retired by a provider no longer requires
  a new release. The `IONET.IOIntelligence` package is gone; the client is a few dozen lines
  over `HttpClient`, because OpenRouter ships no .NET SDK.
- **Visual refresh of the dark theme.** Colours were hardcoded in over thirty places across
  five view files; they now live in a single `Styles/Palette.axaml`. Every text/background
  pair meets the WCAG AA contrast threshold — the timestamps in message bubbles were at
  2.26:1, well below readable. Message bubbles got rounded corners and breathing room, code
  and console output use a monospace font, and the buttons in the message panel finally have
  text labels instead of bare icons.
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

### Fixed

- **Errors from the AI service are now named.** A rejected key, exhausted credit, a rate
  limit and a dead provider all produced the same sentence, "check your API key and network
  connection" — advice that is simply wrong when the account is out of credit.
- **A reply with empty content no longer crashes the send.** OpenRouter can return HTTP 200
  with an error in the body, or a choice whose content is null; the previous client called
  `Choices.First()` on it.
- **`bash` never worked on Windows.** The launcher resolved to the WSL launcher in `System32`,
  because Git for Windows deliberately keeps its `bin` off the system PATH.
  Git Bash is now located through the registry and the script path is translated to the form
  the shell expects — `/c/...` for Git Bash, `/mnt/c/...` for WSL.
- **`groovy` never worked on Windows.** The distribution ships `groovy.bat`, and starting
  a process with `UseShellExecute=false` does not consult PATHEXT — it only appends `.exe`.
- **PowerShell scripts broke on any path containing a space**, which includes every temp
  path with a user name in it: the argument was passed without `-File`, so PowerShell treated
  it as a command rather than a script.
- **PHP scripts silently did nothing.** Without a leading `<?php` the interpreter prints the
  source as text and exits with code 0, so the application reported success. The tag is now
  added when missing.
- **Shell scripts failed on Windows line endings.** A carriage return at the end of every
  line makes a POSIX shell report "command not found"; line endings are now matched to the
  interpreter.
- A missing interpreter reported a raw Win32 error instead of saying which names were tried.
- `zsh` and `batch` now state plainly that they are unavailable on the current system instead
  of failing when the process starts.
- The model saved in the database was not preselected in the settings window: the selection
  was assigned before the list was populated, so the combo box discarded it and saving the
  settings then wiped the choice.
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
- **Packaging failed without a signing certificate**, even though the release workflow
  explicitly handles the "no secret → unsigned package" path. The packaging project set
  `AppxPackageSigningEnabled` to `True` unconditionally, which made the conditional
  default of `False` below it unreachable, and the build stopped with `APPX0101`.
- **The packaging project could not be restored at all** after central package management
  was introduced: its `PackageReference` carries a version inline, which is `NU1008` under
  CPM. The opt-out has to live in `Directory.Packages.props`, because that file is imported
  after `Directory.Build.props` and overrode the setting there.
- **`global.json` rolled forward to any newer major SDK.** As soon as .NET SDK 10 appeared
  on a machine it was selected, and MSBuild 17 from Visual Studio 2022 refuses to work with
  it — so MSIX packaging broke without a single change in the repository. Roll-forward is
  now limited to the 9.0 feature bands.
- `Main` was declared `async Task` without a single `await`. Besides the warning, `async`
  combined with `[STAThread]` is actively wrong: a continuation resumes on a pool thread
  that is not STA.

[Unreleased]: https://github.com/Mysttic/AIDemon2/compare/v2.1.1...HEAD
[2.1.1]: https://github.com/Mysttic/AIDemon2/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/Mysttic/AIDemon2/compare/v2.0.1...v2.1.0
[2.0.1]: https://github.com/Mysttic/AIDemon2/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/Mysttic/AIDemon2/releases/tag/v2.0.0
