# GRL bootstrap handoff V1

## 1. Scope

Owner selected `github-runner-local` and authorized repository setup, issues/PRs, continuity docs and rough design. This handoff is being published during bootstrap; consult main CURRENT and final Issue #1 publication before acting.

## 2. Product

Windows wizard, browser GitHub login, ordinary Documents folder, one runner, explicit on-demand UAC, GitHub request/result channel. Public source is not a public execution mailbox.

## 3. Authority

Main `AGENTS.md` and `docs/CURRENT.md`; Issue #1 for coordination; Issue #2 / GRL-001 for independent planning. No production worker is assigned.

## 4. Known initial state

Fresh reads found an empty public repository with push/admin permission through the owner's GitHub connection, no branches, and no issues/PRs before bootstrap. Initial README commit: `c4b527d157855b84d6c0fd447d3910b5a4e80779`.

## 5. Work in this session

Publish foundation on main; publish a separate rough-design DRAFT PR; locally check documents/prototype; update CURRENT and release the bootstrap claim. No installable product is implied.

## 6. Decisions

See the brief and decision ledger. Private execution hub, UI stack, auth mechanism, protected service layout, signing and license remain open/proposed. One-repo registration cannot route across all personal repos merely through labels.

## 7. Safety

No untrusted jobs, public runner attachment, arbitrary command input, admin CI, policy bypass, token publication or source auto-push. Documents is not a sandbox. GUI/UAC does not prove blocked job shells work.

## 8. Evidence

Primary platform references were read. No real runner, Windows UAC/service, credentials, OAuth app or workflow was exercised. Local validation and design publication refs will be recorded in the successor publication update.

## 9. Next bounded task

After bootstrap publication, owner relays one prompt to Opus 5.5 for GRL-001. Planner reads the full selected design and posts a complete plan on Issue #2. It must not implement, merge, release or dispatch CI. Stop at `PLAN_READY_FOR_REVIEW` or `NEEDS_DECISION`.

## 10. Claim and end

Bootstrap claim is Issue #1 comment `5807377508`. Follow its latest release/successor. Do not assume this chat remains active or that a finished GitHub job wakes an idle chat.

END_OF_GRL_HANDOFF key=GRL-20260924-BOOTSTRAP-V1 sections=10
