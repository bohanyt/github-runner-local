# Decision ledger

Use `ACCEPTED` only for explicit owner direction or an approved technical decision. Technical proposals stay `PROPOSED` until reviewed. References describe platform facts, not product approval.

| ID | State | Decision / question |
|---|---|---|
| D01 | ACCEPTED | Exact name `github-runner-local`; public distribution/source repository. |
| D02 | ACCEPTED | Windows GUI wizard; browser/device login; reusable across machines/accounts. |
| D03 | ACCEPTED | One runner initially; no multi-runner installation in the initial scope. |
| D04 | ACCEPTED | Prefer one local Documents folder on C:; do not silently use a redirected/synced folder. |
| D05 | ACCEPTED | Explicit local UAC only for bounded administrative setup operations; portable mode may legitimately need no UAC. |
| D06 | ACCEPTED | GitHub request/result bridge; no direct chat-to-PC shell. |
| D07 | ACCEPTED | Publish continuity + rough design, then independent Opus 5.5 planning. Completed by plan comment `5807784901`. |
| D08 | ACCEPTED | Portable/trusted-code mode first; service/elevated-helper mode is deferred until after portable acceptance and a separate privilege/ACL review. |
| D09 | OPEN | Dedicated private execution repo is the reviewed default, but owner approval is required before creation. Existing project repos are not silently reused. |
| D10 | OPEN | GitHub App device-flow auth is the reviewed default; owner must approve App creation and acceptance-stage visibility. Public App visibility is not yet approved. |
| D11 | PROPOSED | .NET 10 core now; WPF self-contained x64 shell is the reviewed later checkpoint-B direction, subject to Windows proof. |
| D12 | OPEN | Service identity/protected helper location/ACLs remain deferred. No elevated helper may live in a job-writable path. |
| D13 | PROPOSED | Authenticated issue-comment trigger in a later private execution repo; must prove actual connector event delivery and default-branch workflow behavior. |
| D14 | PROPOSED | Exact-SHA allowlisted test/build profiles; explicit structured result comments; no source write-back. Branch containment is not a trust/sandbox guarantee. |
| D15 | OPEN | License and code signing; no paid certificate or permissive license silently selected. |
| D16 | SUPERSEDED | Three concurrent runners, terminal-first setup, and automatic all-repo routing. |
| D17 | REJECTED | Public command inbox, raw shell commands from comments, UAC/policy bypass, or admin CI by default. |
| D18 | ACCEPTED | Opus plan `5807784901` is accepted **for A1 only** with CT corrections in `5807941253`; later live checkpoints retain their gates/open decisions. |
| D19 | ACCEPTED | Current operational constraint: **no GitHub-hosted Actions** until the owner explicitly re-enables them. A1 uses worker-local compute only. |
| D20 | ACCEPTED | A1 Windows path policy must be host-independent explicit Windows semantics; Linux test success is only `LOCAL_CHECKED`. |
| D21 | OPEN | Stage-2 multi-repo checkout credential/minting authority is not sufficiently designed for activation; first live acceptance must remain inside the private execution repo fixture. |
| D22 | PROPOSED | Official runner initial package is pinned by reviewed digest, while default auto-update may later change the installed version; management operations must detect and gate unsupported versions rather than guess/downgrade. |
| D23 | ACCEPTED | Owner authorizes Checkpoint B per GRL-005 (#8): .NET 10 WPF `net10.0-windows` x64 framework-dependent wizard shell with fake adapters only; no live GitHub/auth/runner/OS-probe/process/network behavior. Source: Issue #1 comment `5809860108`. D11 self-contained packaging remains PROPOSED. |
| D24 | ACCEPTED | Evidence labels for Checkpoint B: headless tests and fake-mode UI automation smoke on a worker Windows desktop are `LOCAL_CHECKED` (with qualifier). `WINDOWS_TESTED` is reserved for real-integration acceptance (G1 and gated live checkpoints). Source: Issue #1 comment `5809860108`. |

## Corrections to earlier exploratory conversation

- Runner labels select among accessible runners; they do not make a repository registration account-wide.
- Runner execution has the operating-system identity's access, not a filesystem sandbox around `_work`.
- A GUI that invokes UAC does not prove that blocked shells, build tools or child processes will be permitted.
- Normal Actions status/log publication is distinct from custom issue comments/artifacts and explicit source pushes.
- A finished workflow does not itself resume an idle chat. Result reading and notifications need explicit supported integration.
- Same-user credential encryption is storage protection, not isolation from that user's untrusted build processes.
- Exact-SHA/branch-containment validation prevents stray refs; it does not make allowed code trustworthy.
- A pinned initial runner plus default auto-update is not an end-to-end immutable runner version.
