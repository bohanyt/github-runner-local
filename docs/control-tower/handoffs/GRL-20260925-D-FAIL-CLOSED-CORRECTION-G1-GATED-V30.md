# github-runner-local — D fail-closed correction, office G1 gated (V30)

## 1. Phase

**D_FAIL_CLOSED_CORRECTION_READY; OFFICE_G1_GATED.** The previous attempt to correct R-D-1 is blocked by a missing safe portable Drain contract. CT has narrowed D source to a truthful fail-closed lifecycle. There is no merge or live G1 authority.

## 2. Exact authority

Public main at CT claim: `7305a75120313153391697bd2ff46dad37f89974` (V29); decision-ledger advance `388a76ba3c987bb13f6dbde54060296fe87df806`. DRAFT PR #17 branch `feat/grl-d-portable-lifecycle`, head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`, remains open/unmerged and unchanged at contract decision. Original D source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`.

Issue #16 original source packet is qualified by CT revision comment `5828534853`, superseding correction packet `5828348757` for R-D-1. Issue #15 G1 cleanup/switching clarification `5828537511`. Decision ledger D31 accepted CT safety decision, D32 open safe switching.

## 3. Block evidence

Independent review Issue #16 `5828083061`: `NEEDS_D_CORRECTION` on R-D-1/R-D-2. Blocked worker handoff `5828419459`; Issue #1 release `5828425051`. Pinned runner v2.337.0 listener Ctrl+C requests shutdown; `JobDispatcher.ShutdownAsync` cancels the running job. Remote idle/absent status is a snapshot, so idle→assigned and absent→assigned races persist. A passing unchanged-head Windows build and 352/352 tests twice do not close these findings. Evidence remains `LOCAL_CHECKED`.

## 4. Revised D implementation

ONE bounded implementation worker uses the SAME branch and SAME DRAFT PR #17, starting from the exact unchanged head. Follow Issue #16 `5828534853`: Drain returns unavailable/pending with no process termination, including absent/idle/timeout status; unregister refuses a live owned process and never invokes Stop Now implicitly; explicit Stop Now is owned-process-only with job-cancellation warning; partial configure/start/online failure leaves a usable live UI recovery/removal path with identity checks and pending states. Fake UI remains visibly simulated. Only Issue #16's existing source/test allowlist applies. No Core/schema/template/authority edits on the worker branch.

## 5. Review and evidence gate

Worker runs restore, warnings-as-errors build, both full solution test runs, read-only runner witness and diff check; deterministic tests must cover idle→assigned and absent→assigned interleavings, unregister no-kill, UI recovery, Stop Now distinction and redaction. Publish exact base/head/paths/results/limits on Issue #16; release claim. DIFFERENT independent reviewer rereviews the new exact head. Do not mark the graceful Drain capability complete. CT separately decides merge after an exact-head PASS.

## 6. Deferred safe switch

Design Issue #18 tracks a supported admission fence, completed in-flight work proof, exact runner identity and two-laptop switching procedure. There is no automatic safe Drain or scheduled switch in D source. Office G1 may later prove first-run same-repo execution under Issue #15, but does not prove a safe handoff to `grl-personal`. Owner's D30 conditional enrollment approval after G1 PASS remains; activation waits for a proved one-active-at-a-time execution packet.

## 7. Live boundaries

Issue #15 stays GATED while PR #17 is draft/unmerged or findings uncorrected/unreviewed. Its failed-enrollment cleanup cannot silently Stop Now or unregister an active process. No live GitHub login, execution-repo template activation, runner registration/start, hosted Actions, G1 witness, personal enrollment, Stage-2/cross-repo credential, service/UAC, signing or release is authorized by this handoff.

## 8. Next

Fresh-check main CURRENT, PR head, Issue #16/#18 and Issue #1 claims. Dispatch one bounded D fail-closed correction worker under Issue #16 `5828534853`; then a different independent reviewer. CT retains merge/G1 gate. If a supported safe drain is discovered, return evidence to Issue #18 for a separate reviewed design rather than silently changing this packet.

END_OF_GRL_HANDOFF key=GRL-20260925-D-FAIL-CLOSED-CORRECTION-G1-GATED-V30 sections=8
