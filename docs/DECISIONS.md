# Decision ledger

Use `ACCEPTED` only for explicit owner direction or an approved decision. Technical proposals stay `PROPOSED` until reviewed. References describe platform facts, not product approval.

| ID | State | Decision / question |
|---|---|---|
| D01 | ACCEPTED | Exact name `github-runner-local`; public distribution/source repository. |
| D02 | ACCEPTED | Windows GUI wizard; browser/device login; reusable across machines/accounts. |
| D03 | ACCEPTED | One runner initially; no multi-runner installation in the initial scope. |
| D04 | ACCEPTED | Prefer one local Documents folder on C:; do not silently use a redirected/synced folder. |
| D05 | ACCEPTED | Explicit local UAC only for bounded administrative setup operations. |
| D06 | ACCEPTED | GitHub request/result bridge; no direct chat-to-PC shell. |
| D07 | ACCEPTED | Publish continuity + rough design, then independent Opus 5.5 planning. |
| D08 | PROPOSED | Portable/manual runner mode first, service only after privilege/ACL design. |
| D09 | OPEN | One-repository pilot versus private execution hub for multiple personal repositories. A label cannot expand registration scope. |
| D10 | OPEN | GitHub CLI browser auth versus a separately registered OAuth/GitHub App; exact permissions and storage. |
| D11 | OPEN | Native UI stack/runtime, supported Windows editions, distribution and update format. |
| D12 | OPEN | Service identity, protected helper location, authenticated IPC and Documents preference conflict. |
| D13 | PROPOSED | Authenticated issue-comment trigger through a private execution target; prove actual connector actor/event delivery. |
| D14 | PROPOSED | Exact-SHA allowlisted test/build profiles; explicit structured result comments; no source write-back. |
| D15 | OPEN | License and code signing; no paid certificate or permissive license silently selected. |
| D16 | SUPERSEDED | Three concurrent runners, terminal-first setup, and automatic all-repo routing. |
| D17 | REJECTED | Public command inbox, raw shell commands from comments, UAC/policy bypass, or admin CI by default. |

## Corrections to earlier exploratory conversation

- Runner labels select among accessible runners; they do not make a repository registration account-wide.
- Runner execution has the operating-system identity's access, not a filesystem sandbox around `_work`.
- A GUI that invokes UAC does not prove that blocked shells, build tools or child processes will be permitted.
- Normal Actions status/log publication is distinct from custom issue comments/artifacts and explicit source pushes.
- A finished workflow does not itself resume an idle chat. Result reading and notifications need explicit supported integration.
- Same-user credential encryption is storage protection, not isolation from that user's untrusted build processes.
