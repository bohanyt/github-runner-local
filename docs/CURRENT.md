# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **C_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW + E_SOURCE_IMPLEMENTATION_ACTIVE**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for parallel E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-C-IMPLEMENTED-REVIEW-READY-E-ACTIVE-V13.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-C-IMPLEMENTED-REVIEW-READY-E-ACTIVE-V13 sections=9`.

## Checkpoint C candidate

- Issue #10 / GRL-009.
- Branch: `feat/grl-c-device-runner-package`.
- DRAFT PR #12: open, draft, unmerged, mergeable.
- Exact base: `cd6f6abd13de9af2122d1c61db774b2dc54f7da0`.
- Exact candidate head: `67dd220771bf67f65348e622998e005bf30b26bc`.
- Implementation handoff: Issue #10 comment `5813138169`.
- Implementation claim `5812908520` released by `5813145195`.
- Diff: exactly 13 paths, all within the GRL-009 C allowlist.

## C evidence

Worker-local non-elevated Windows evidence:
- solution restore/build PASS, zero warnings/errors;
- Core 192/192;
- Presentation 45/45;
- Integration 45/45;
- zero failed/skipped;
- Windows read-only RunnerContractProbe PASS;
- official runner v2.337.0 / `actions-runner-win-x64-2.337.0.zip`;
- SHA-256 `1150692afa94e71f872017e254ea55b6eece1eece3fe7e3a6d4c93d0a1b85cfc` matched;
- Listener reported `2.337.0`;
- `config.cmd --help` exposed all 12 required capabilities.

Evidence remains `LOCAL_CHECKED`; official runner metadata is `SOURCE_VERIFIED`.

## Independent C review gate

One independent reviewer must review exact PR #12 head
`67dd220771bf67f65348e622998e005bf30b26bc`.

Review must cover the full 13-file C candidate against Issue #10, including:
- device-flow HTTP/state/error/token-redaction semantics;
- repository private/admin admission;
- runner pin trust and URL/hash/download/safe-extraction containment;
- reparse/symlink/traversal/device/ADS/size/cleanup protections;
- runner CLI capability/version/argv constraints;
- deterministic test quality and meaningful negative coverage;
- Windows contract-probe source safety and whether reported evidence coheres;
- exact write scope / no E overlap / no live activation.

If PR #12 head moves, the review target is stale.

No C merge is authorized before independent PASS.

## Checkpoint E parallel state

Issue #11 / GRL-010 remains independent and path-disjoint.

Active E implementation claim:
Issue #1 comment `5812970520`.

E branch:
`feat/grl-e-execution-template`.

E write scope remains:
`templates/execution-repo/**` ONLY.

E-B1 resolution remains Issue #11 comment `5812014790`.

This continuity update is authority/docs-only. An E worker that observes main movement caused by this C continuity transaction must not treat it as a source conflict or silently merge/rebase unless its authority requires that.

## Open gates

Still OPEN:
- D09 / OD-1 private execution repo;
- D10 / OD-2 GitHub App creation/visibility;
- D21 Stage-2 credentials;
- D12 service identity/helper;
- D15 license/signing.

No C merge, live login/App, runner registration/start, live E activation, hosted Actions, D/G1/service/release work is authorized.

## Constraints

PR #3 remains untouched. No hosted Actions. No root workflow activation. No repository setting, security, power, or policy changes.

## Next

Run one independent exact-head review of DRAFT PR #12. Publish one review verdict on Issue #10, release the review claim, then stop. In parallel, the already-claimed E worker may continue its own Issue #11 source task.

