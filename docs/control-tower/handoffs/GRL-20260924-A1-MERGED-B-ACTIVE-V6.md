# github-runner-local — A1 merged, Checkpoint B active (V6)

## 1. Phase

**A1_MERGED / B_IN_PROGRESS_UNDER_LEASE.** Checkpoint A is merged and verified locally. The owner authorized one bounded lease for A1 closeout, continuity repair, and GRL-005 implementation. No Checkpoint B implementation result or independent review exists yet.

## 2. Authority and read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this entire handoff → Issue #1 latest comments and owner authorization `5809860108` → Issue #8 GRL-005 packet. The Opus plan is Issue #2 comment `5807784901`; CT review is comment `5807941253`. Use immutable refs below and fresh-check mutable claims and refs before writes.

## 3. A1 lineage and merge

- Implementation: Issue #4 comment `5808741650`; original review: `5809108582` (`NEEDS_A1_CORRECTION`, R-A1-1); correction: `5809246654`; independent rereview: `5809464438` (`PASS_A1_CORRECTION_EXACT_HEAD`, R-A1-1 closed).
- PR #5 reviewed head `e21599eda1d4c40c391c2d34546e616e59645144`, original base `3e02566f5dcaeea01f5f34284e21731c71b1144b`, merged using method `merge` at `49569022b7773c12ebd3681f2ddc7f40f9826f2e` (M). M's parents are prior main `68ad9ea3ee3cfa81da91d4edb7400fb9ac8eb5a5` and the reviewed head. The 27 merged A1 paths and their contents match the reviewed head.
- Post-merge local Windows SDK 10.0.401: `dotnet build -warnaserror` exit 0, 0 warnings/errors; `dotnet test` exit 0, Core 192/192 passed, 0 failed, 0 skipped. Evidence: `LOCAL_CHECKED` only.
- Issue #4, Issue #6 and Issue #7 were closed as completed. PR #3 remains open/draft and proposed.

## 4. Continuity repairs in T1

`AGENTS.md` now points to main CURRENT and the active task without embedding stale task heads. CURRENT selects GRL-005 and this V6 handoff. `docs/DECISIONS.md` appends owner-authorized D23 (fake-only WPF B) and D24 (evidence labels); existing rows remain unchanged. The Issue #8 packet and `docs/tasks/GRL-005-b-wpf-shell.md` mirror define the bounded B work. T1's own commit SHA is published on Issue #8, not embedded here.

## 5. Active task and lease

Issue #8 is **GRL-005 — Checkpoint B: WPF wizard shell with fake adapters**. The task branch is `feat/grl-b-wpf-shell` from T1, with one DRAFT PR to main and one B implementation handoff. Issue #1 claim `5809865391` holds `GRL-SOL-LEASE-A1-CLOSEOUT-B-20260924` until `2026-09-24T17:35:48Z` or release, with soft cutoff `2026-09-24T15:35:48Z`. The lease holder implements but does not review its own work.

## 6. Constraints

No GitHub-hosted Actions or workflow changes. B uses fake adapters only: no live GitHub/auth, runner download/registration, real OS probes, process launch from product code, token storage, GitHub App/execution repo, UAC/service, machine-policy/security change, release, or later checkpoint work. Do not touch PR #3. B's proof ceiling is `LOCAL_CHECKED`; fake-mode UI smoke does not establish real integration acceptance.

## 7. Known stale items not fixed

`CONTRIBUTING.md` still has older phase wording, and the last paragraph of `docs/control-tower/ROLE.md` is stale. Neither is in T1's allowed file set. Main CURRENT and the Issue #8 packet govern this lease.

## 8. Next

The lease holder implements Issue #8 on `feat/grl-b-wpf-shell`, runs the local evidence campaign, publishes one DRAFT PR and a complete handoff, then updates CURRENT to B implemented / awaiting independent review and releases the claim. Owner/CT later creates the independent exact-head B review packet. No self-review, merge of B, or Checkpoint C work is authorized.

END_OF_GRL_HANDOFF key=GRL-20260924-A1-MERGED-B-ACTIVE-V6 sections=8
