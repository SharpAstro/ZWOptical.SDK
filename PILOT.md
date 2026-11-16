# PILOT.md

What a plane can and cannot do in this repo. The node quotes this page whole into the planner, worker and reviewer
briefs, so it stays short. `CLAUDE.md` holds the architecture and conventions. This page holds only what a flight needs to plan, build, check and land.

ZWOptical.SDK is the .NET binding for ZWO (ASI cameras, EAF focusers, EFW filter wheels): the vendor's native libraries under `lib/`, and the managed code
over them in `include/`. It has no tests, because everything it does ends in a call into a camera.

## What a plane can and cannot do

- A plane is a git worktree at `<repo>/.claude/worktrees/<code>` on `flight/<code>`. Work and commit only there. Do not
  push, open a PR or merge; the controller lands the flight. Leave nothing uncommitted, and put scratch output
  outside the plane.
- **A plane has no hardware and validates nothing on hardware.** It never opens a camera, never runs a probe tool and
  never loads the vendor's library to see what it does. What a change does on a real body is not known until the owner
  has tried it on the bench: the leg's report says so in those words, and never says a change "works".
- **What a plane is for here:** moving a dependency pin (TianWen.DAL, the SDK's analyzers, the workflow's actions),
  the CI workflow, the README and comments, and a binding change whose correctness can be read off the code and the
  vendor's header or manual (a missing guard, a buffer sized from the wrong number, a call made in the wrong order).
- **What a plane leaves alone:** anything whose answer is the camera's (does this call reset the window, does that
  one flush the queue, what does the body return when asked twice). Name the question in the report and stop; do not
  guess a default. Do not change a native signature or a struct layout unless the vendor's header in this repo shows
  the current one is wrong, and quote the header's line.
- Never add, replace or delete a file under `lib/`. The vendor's libraries are taken by the owner, with their
  source recorded.
- `demo/` is the vendor's own sample code, kept for reference. Do not build, edit or reformat it.
- TianWen.DAL is taken as the pinned package in a plane (the sibling clone is not beside a worktree), which is what CI
  builds. Its consumer, `tianwen`, cannot be built from a plane: a change's effect on it is named in the report, not
  checked, and a change `tianwen` must make is its own flight in that repo.
- Never wait with a job that sleeps or polls: use `pilot leg wait --for-file <path>` and end the turn. Long builds go
  through `pilot run -- <command>`. The machine is shared, so a job may wait its turn first.

## Build and check

Always `-c Release`, as CI does. Run from the repo root.

```bash
dotnet build ZWOptical.SDK.sln -c Release
```

- The build is the whole check a plane has: it must end with no error and no new warning. There is no test project,
  and a plane does not add one that needs a camera.
- A change to the public surface is checked by reading its callers in `tianwen`
  (`src/TianWen.Devices.Native`, on GitHub), and the report names each one it breaks.

## Compatibility and release

- **Merging to `main` publishes** the package (CI pushes to nuget.org on every push to main, the patch segment its own). There is
  no separate release step, so what lands on main ships.
- A change of behaviour or API bumps `VersionMajorMinor` in `Directory.Build.props` (additive is a minor, a break is a
  major), and the commit's subject ends with the new version in parentheses, as the log's do. A change to comments,
  the README or this page bumps nothing.
- Every vendor binding pins the same TianWen.DAL. A flight that moves the pin here says so in its report, so the other
  bindings follow in their own flights.
- AOT and trimming must stay clean: no reflection. A new native call follows the file it is added to (`LibraryImport`
  or a function pointer; `DllImport` only where the file already uses it for a struct that cannot be marshalled
  otherwise).

## How a flight merges

- main has no merge queue, but its ruleset requires **linear history**, so a PR lands by squash or rebase, never as a
  merge commit. Take main by a merge (`git fetch origin`, `git merge origin/main`), never a rebase, and never push with
  `--force`. A merge that resolved a conflict cannot be rebased onto main; say so in the leg's report, so the PR
  lands squashed.
- Keep a flight's commits few and each one whole.

## Writing

No em-dashes, and no `--` in their place: a full stop, a comma, a semicolon or parentheses. This holds for prose,
comments and commit messages.
