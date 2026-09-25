# Architecture proposal v0

Status: PROPOSED. Platform sources are indexed as R1-R11 in main `docs/REFERENCES.md`. Open questions must be resolved by GRL-001 before implementation.

## 1. Separate product from execution

```text
PUBLIC PRODUCT REPO                 PRIVATE / AUTHORIZED EXECUTION REPO
source + docs + future releases      workflow + requests + reports
              |                                  ^
              v                                  | GitHub connector
Windows GUI wizard                              chat agent
(normal user)                                    |
              | enroll, local consent            v
              v                          GitHub Actions queue
Official runner client <-------------------------+
(least-privileged job identity)
              |
              v
trusted checkout -> build/test -> logs/status/report -> GitHub
```

The public product repo is not the owner's live job mailbox. No execution repo is created by this design. No public PR can enroll or dispatch a machine. Other users can install the future product and authorize their own scope.

## 2. One runner and many repositories

R1 establishes three relevant choices:

| Choice | Fits one runner? | Cost/constraint |
|---|---|---|
| One private repository pilot | Yes | Direct workflow and token scope; proves the loop for one repo only. |
| Private execution hub | Yes, registered to the hub | Hub jobs explicitly fetch approved target repos; separate cross-repo permissions and reporting are required. |
| Organization runner | Yes, for permitted org repos | Does not cover arbitrary personal-account repos; moving repos is not authorized. |
| Multiple repo registrations | Multiple runner instances | Conflicts with the current one-runner requirement; not the default. |

Recommendation: prove a harmless one-repo pilot, then make the hub an explicit owner decision. Never imply identical labels make one personal repo registration account-wide. A hub Actions check is not automatically a target PR required check; initial return can be a hub report linked by the agent. Target checks need an additional explicit design.

## 3. Components

- **Wizard/controller:** normal-user native Windows UI; account/scope/path selection, setup status, pause/drain, safe disconnect and diagnostics.
- **Auth adapter:** GitHub CLI browser/device login or a correctly registered application, chosen by the planner. No proprietary chat credential reuse.
- **Runner adapter:** obtain an official pinned package, verify its digest, configure and supervise the runner. Inspect the actual chosen package: internal executables are not an assumed stable public API.
- **Bounded setup helper:** request local UAC for install/remove/repair service operations only. No arbitrary executable, command or unrestricted path arguments.
- **Workflow/profile layer:** validated GitHub request -> exact checkout -> fixed profile -> result. No model inference or arbitrary command generation on the laptop.
- **Result publisher:** bounded summary and explicit optional artifacts. Keep request/run/attempt/target identities intact.

C# with WPF or WinForms is a candidate, not a settled runtime/framework. The planner must choose a supported runtime, deployment size and update strategy. The HTML mock is not a production web wrapper decision.

## 4. Identity boundaries

GitHub sign-in identity, GitHub runner registration permission, local Windows user, administrative setup helper, job identity and result-publishing credential are different things. Prove each independently.

Separate short-lived registration credentials from runtime runner state and target-checkout credentials. The workflow's GITHUB_TOKEN is scoped to its own repository (R6). Cross-repository checkout or comments require deliberately granted access. A later hub must explain where its credentials reside and which job code can read them.

Do not run jobs in the wizard's elevated process. Prefer a dedicated non-admin identity for real jobs. Portable same-user mode may be offered only as an explicitly trusted-code mode with clear access warnings. Encryption under the same user does not isolate secrets from that user's build processes.

## 5. Folder layout and service conflict

Portable-mode candidate:

```text
C:\Users\<user>\Documents\github-runner-local\
  GitHubRunnerLocal.exe       proposed UI artifact
  runtime\                  official runner package and local state
  work\                     bounded job checkout/output
  logs\                     bounded, redacted local diagnostics
  settings.json              non-secret preferences
```

Paths shown are proposals, not existing files. Do not clone/reuse runner identity across laptops. Credentials are never versioned; portable distribution does not mean portable credentials.

An elevated service/helper must not trust executables or privileged configuration writable by job code. Protecting a subfolder's files without protecting ancestor replacement is insufficient. A Program Files / ProgramData split may be required and conflicts with the one-Documents-folder preference. The planner must present a safe concrete layout or defer service mode; no silent exception and no LocalSystem shortcut.

Resolve the Windows Documents known folder, detect OneDrive/network redirection, show the final path and obtain consent for a local C: alternative. Never change the user's Windows folder policy.

## 6. Lifecycle

Preflight before enrollment: supported OS/architecture, actual shell/tool execution, allowed outbound endpoints, writable local folder, free disk, running-instance conflict and authorization. Stop on endpoint-policy denial. UAC success is not proof that the runner's job subprocesses will be allowed (R8-R10).

Keep one job slot. Pause means stop taking new work after draining the current job; immediate cancel is separate and explicitly confirmed. Reconnect reconciles local state with GitHub runner identity. Never overwrite another machine's runner merely because its display name matches.

Uninstall distinguishes stop, unregister, local credential removal, service removal and optional workspace deletion. On offline/unregister failure, preserve a clearly marked pending action. Account switching drains work and reauthorizes; it must not silently remove another app's shared CLI credential.

## 7. Speed and storage

Record queue, checkout/restore, build, test and report durations separately. Persistent caches can help but must be bounded and invalidated by lockfiles/toolchain. One runner means one job, not one compiler thread. Measure job-level parallelism rather than forcing maximum CPU.

With constrained disk, propose a configurable reserve (initial discussion value: 15 GiB), a per-job admission estimate and bounded caches/artifacts. The reserve is not a guarantee that a build fits. Detect exhaustion during a job; preserve small diagnostics. Cleanup must stay inside owned directories and reject reparse/symlink escapes. No deletion of user Documents, global package caches or other projects.

## 8. Non-goals and proof

No always-on free VM, remote desktop, AI agent runtime, admin job executor, network share, sleep/lock-policy changes, GPU speed guarantee, or native Windows support for Linux container actions (R2). Existing project CI is not migrated by bootstrap.

First proof is a harmless known fixture through a private scope and an owner-approved machine. The planner must split source/mock checks, Windows behavior, actual GitHub round-trip, and owner acceptance. See [acceptance](ACCEPTANCE.md).
