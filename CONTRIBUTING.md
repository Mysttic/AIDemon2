# Contributing

The product itself is described in [README.md](README.md). This file is for people
changing the code.

## Requirements

- Windows 10/11 **64-bit** and the .NET SDK pinned in [global.json](global.json)
- **Visual Studio 2022 (or Build Tools) with the *MSIX Packaging Tools* component**
  if you need to build the installer. Without it `AIDemon2.Package.wapproj` fails
  with `MSB4019: Microsoft.DesktopBridge.props not found`.

## Building and testing

```bash
dotnet build AIDemon2/AIDemon2.csproj -c Release -p:Platform=x64
dotnet test  AIDemon2.Tests/AIDemon2.Tests.csproj
```

**Do not build `AIDemon2.sln` with `dotnet`.** The solution contains the packaging
project, whose DesktopBridge targets only exist inside a Visual Studio installation,
so `dotnet build AIDemon2.sln` always fails. Build the two projects separately, or
use `msbuild` for the whole solution.

The test suite runs entirely on fakes and an in-memory SQLite database: no network,
no API key, no GUI. That is deliberate — it has to run on CI, which has none of those.

A few notes on the tests:

- The database fixture uses **real SQLite in memory, not the EF InMemory provider**.
  InMemory does not execute SQL, so it would not catch a broken query filter or a
  `DateTimeKind` lost on round-trip — exactly the defects these tests exist for.
- Migration tests are slow (~20 s) because SQLCipher derives a key with PBKDF2.
- The OpenRouter client is tested against a fake `HttpMessageHandler`, so no test needs
  an API key or makes a paid request.
- `CodeRunnerIntegrationTests` starts real interpreters. Languages missing on the machine
  are skipped rather than failed — the set installed on a developer box and on CI differs.

## Checking the script languages

Interpreter configuration lives in `AIDemon2/Properties/ProgrammingLanguages.json` and is
verified end to end, because a wrong launcher shows up only when a user tries to run code:

```bash
docker build -t aidemon-langcheck tools/language-check
docker run --rm -v "$(pwd):/repo:ro" aidemon-langcheck bash /repo/tools/language-check/run.sh
```

The container covers the Linux side (10 languages). PowerShell and batch are Windows-only
and are covered by the integration tests instead.

## Architecture

Everything is built as **x64** — `PlatformTarget` in both project files. That single
property decides the bitness; `-p:Platform=x64` only picks the output directory, and
`Platforms` is just the list offered to the IDE. The test project must match the
application, or the test host fails with a misleading
`Could not load file or assembly AIDemon2` despite the DLL being present.

Note that `RuntimeIdentifiers` does **not** trim the `runtimes/` folder in a plain
`dotnet build` — the output still carries all 25 platforms (~130 MB). Trimming needs
a RID-specific build (`-r win-x64`).

## Releasing

**The version is set in [VERSION](VERSION) and nowhere else. Tags are created by
automation — never by hand.**

1. On `develop`, bump the number in `VERSION` and describe the changes in
   [CHANGELOG.md](CHANGELOG.md) under `## [Unreleased]`.
2. Open a PR `develop` → `master` and merge it once CI is green.
3. That is all. The rest is automatic.

Merging without changing `VERSION` releases nothing — the change lands on `master`
and waits for the next release.

### What the automation does

[release.yml](.github/workflows/release.yml) runs on every push to `master` and:

1. reads the number from `VERSION`;
2. **stops with no effect if the tag `vX.Y.Z` already exists** — this is the gate
   that makes unrelated merges harmless and re-runs safe;
3. builds and runs the full test suite;
4. moves the `[Unreleased]` section under `## [X.Y.Z] - date`, opens a fresh empty
   `[Unreleased]`, and refreshes the reference links;
5. writes `X.Y.Z.0` into `Package.appxmanifest`;
6. commits those two files back to `master`;
7. creates the tag `vX.Y.Z`;
8. builds the MSIX package and publishes a release with the changelog section as
   its body.

Check locally before opening the PR:

```bash
pwsh tools/Release.ps1 -Command check    # can the current state be released?
pwsh tools/Release.ps1 -Command notes    # preview the release body
```

### Version numbers

The MSIX version is `Major.Minor.Build.Revision` and the Store requires `Revision`
to be `0`, so the SemVer patch maps onto `Build` and the automation appends `.0`.
`VERSION` itself stays three-part.

| Change | Part |
|---|---|
| removing or repurposing a setting, dropping an export column | MAJOR |
| new feature, new setting, new supported script language | MINOR |
| bug fix, performance, wording, docs | PATCH |

### Signing

The certificate is **not** in the repository, and the packaging project no longer
pins a certificate thumbprint — that thumbprint referenced a certificate present
only in one developer's store, which is why every build elsewhere failed.

Signing is driven by two repository secrets:

| Secret | Contents |
|---|---|
| `MSIX_PFX_BASE64` | the `.pfx` file, base64-encoded |
| `MSIX_PFX_PASSWORD` | its password |

The certificate subject **must** be exactly `CN=7D16EB93-BD30-4D8E-A3B6-8FDB3CE89F4A`,
matching `Publisher` in `Package.appxmanifest`, or signtool rejects the package.

Without the secrets the release still completes, but the resulting MSIX is **unsigned and
therefore not installable**. Windows refuses a package with no trust chain outright — this
is not a warning a user can click through, and no bundled script works around it. That is
why every release also ships a portable ZIP, which is the artifact users actually run.

Sideloading is not the obstacle here: it has been on by default since Windows 10 version
2004. The missing piece is the signature.

If you do want an installable MSIX, the options are, cheapest first:

| Option | Cost | What the user does |
|---|---|---|
| Microsoft Store | free (account fee was dropped) | one click, no warning, auto-updates |
| Azure Artifact Signing | ~$10/month | double-click the MSIX, Install |
| OV certificate from a CA | from €69 (OSS pricing) | double-click the MSIX, Install |

Note that an EV certificate no longer bypasses SmartScreen — Microsoft retired that
behaviour, so paying the EV premium for that reason alone buys nothing.

### Repository settings this needs

- `Settings → Actions → General → Workflow permissions` set to
  *Read and write permissions*, otherwise the automation cannot push its commit or tag.
- If `master` is protected, allow `github-actions[bot]` to bypass the pull-request
  requirement, otherwise the "Commit release changes" step is rejected.

### Undoing a release

Do not delete published tags — someone may already have downloaded the package.
Ship a PATCH release with the fix instead, and mark the faulty release as a
pre-release with a note explaining the problem.

## Where things live

`AIDemon2/` is a single project. A few landmarks:

- `Domain/` — EF Core context, entities, repositories. `DatabaseKeyProvider` owns
  the SQLCipher key; `DatabaseLocation` owns where the database lives.
- `Services/` — chat (OpenRouter), model catalog, code runner, dialogs, export, logging.
- `ViewModels/` + `Views/` — Avalonia MVVM, using CommunityToolkit.Mvvm only.

Invariants worth knowing before changing things:

- Property change notification comes from `CommunityToolkit.Mvvm` alone. It used to
  be woven into the assembly by PropertyChanged.Fody, where losing it produced no
  compile error — just dead bindings. `AIDemon2.Tests/Domain/NotificationTests.cs`
  guards every bound property.
- Repositories take `IDbContextFactory` and create a context per operation. They are
  singletons, so injecting a scoped `DbContext` would make it a captive dependency
  living for the whole process.
- Soft delete is enforced by a global query filter, not by remembering to add
  `!Deleted` to each query.
- Code produced by the model is executed locally. Any change there needs a confirmation
  step, a timeout and process-tree cleanup.
