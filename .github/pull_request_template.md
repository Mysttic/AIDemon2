## What and why

<!-- One or two sentences: what changes and why. -->

## Checklist

- [ ] `dotnet build AIDemon2/AIDemon2.csproj -c Release -p:Platform=x64` succeeds
- [ ] `dotnet test AIDemon2.Tests/AIDemon2.Tests.csproj` passes
- [ ] New behaviour has a test; a fixed bug has a regression test
- [ ] Changes described in `CHANGELOG.md` under `## [Unreleased]`
- [ ] README updated if a user-visible feature changed

## Does this PR ship a release?

A release is triggered by **changing the number in `VERSION`** — not by a tag,
not by the merge itself.

- [ ] **Yes** — `VERSION` bumped per SemVer (see [CONTRIBUTING.md](../CONTRIBUTING.md))
      and `pwsh tools/Release.ps1 -Command check` passes
- [ ] **No** — `VERSION` unchanged; the work waits for the next release

## Verified on

<!-- Which AI model, which script language, which database state. CI has no API key
     and cannot exercise the io.net path or the code runner. -->
