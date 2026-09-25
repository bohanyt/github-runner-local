# Bootstrap validation — 2026-09-24

Scope: rough-design documents, invented fixtures and offline HTML preview. **Not the future Windows application.**

## Commands and observed results

Executed against the locally staged design pack in temporary Linux compute:

```text
python tools/check_design.py
PASS: offline design pack, links, invented fixtures and preview guardrails

python -m unittest discover -s tools -p 'test_*.py'
26 tests: OK

python tools/check_preview.py --chromium /usr/bin/chromium
PASS: Chromium 144.0.7559.96
7 panels; 1280x1000 and 390x844 viewports
0 page errors; 0 HTTP(S) requests during the checked interaction sequence
```

The 26 tests exercise the design checker, including bad links, malformed example data, mismatched result identity, contradictory PASS, accidental live workflow and preview-network constructs. They are not production security tests or real CI results.

Browser checks cover navigation, mock sign-in/reset, literal user-text rendering, simulated reporting failure, pause/disconnect/restart and no horizontal overflow in the tested layouts. A desktop screenshot was also visually inspected.

## Environment and publication limitations

Public git transport from temporary compute failed DNS resolution. Files were staged locally and published using the authorized GitHub connector's Git data operations. The final PR/publication report must identify the exact remote commit and compare Git blob IDs against the tested local source; this document does not substitute for that comparison.

The available system Chromium was used because the Playwright-managed browser was not installed. File-URL navigation was blocked by the browser harness policy. The documented test loads HTML through `set_content` without changing that policy. Therefore double-click/file-launch acceptance is **not** claimed.

## Not tested or performed

No Windows binary, Windows UI framework, actual GitHub login, credential store, UAC, Windows service, runner download/registration, repository checkout by a runner, live job, private-repository access, security isolation, or result upload was tested. No installer or release exists. No hosted Actions or self-hosted workflows were dispatched.

The local screenshots are mock previews, not evidence of an enrolled machine. Example result JSON is expressly invented and must not be mistaken for the 26 checker-test results above.
