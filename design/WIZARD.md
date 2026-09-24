# Wizard and dashboard proposal

Status: PROPOSED. The [preview](wizard-preview.html) is an offline mock, not a product binary or real GitHub sign-in.

## Screens

| Step | User sees | Required behavior |
|---|---|---|
| 1. Welcome & preflight | What runs locally; trusted-code warning; machine checks | Permission/policy denial has a clear stop reason, not a bypass button. |
| 2. GitHub account | Sign in in browser; device code fallback; selected account | No password field in this application; actual authenticated account must be verified. |
| 3. Execution scope | Repository or permitted organization; access check | Public product repo is not preselected as an execution mailbox; personal account is not an org. |
| 4. This laptop | Display name, one slot, folder, disk reserve | Show resolved local path, account association and redirected-folder warning. |
| 5. Mode & review | Manual/portable or service; exact changes | Service disabled until its identity/ACL design is approved. UAC is only requested after an explicit setup action. |
| 6. Setup & verification | Download, verify, configure, connection and smoke status | Each stage has real evidence, cancellation and recovery. No optimistic success while report delivery is pending. |
| Dashboard | Status, job, latest result, disk budget, pause/disconnect | No background login or unnoticed scope switch. Open GitHub run/report, not raw local secrets. |

## UI direction

Quiet desktop utility, readable typography, explicit progress and states. Avoid animated fake activity. Buttons describe actions: Sign in through GitHub, Choose folder, Review permissions, Register this laptop, Run connection test, Pause after job, Disconnect. No misleading Connect all my repositories option.

A compact status line distinguishes **Not configured / Configuring / Offline / Idle / Busy / Draining / Paused / Degraded**. The latest run shows request ID, target repository + commit, test outcome and reporting outcome separately. The display name is not a security identity.

## Failure copy examples

- **Administrator approval cancelled:** Nothing requiring administrator permission was installed. Continue in an approved portable mode or retry explicitly.
- **PowerShell blocked by device policy:** The wizard opened, but this profile cannot execute on this PC. Ask the device administrator to approve the required tools.
- **Repository permission missing:** This account can view the repository but cannot register a runner there.
- **Credential store unavailable:** Sign-in cannot be persisted securely under the selected design. No silent plaintext fallback.
- **Disk reserve would be crossed:** Pause admission and show what the application owns and can safely remove.
- **Tests finished; report upload incomplete:** Keep the run as reporting-incomplete, retain bounded local evidence and retry publishing the same run result.

## Account and laptop changes

A new laptop receives its own registration and local credentials. Show existing registrations but do not replace/remove them without explicit confirmation. Signing out of the wizard and unregistering a runner are separate operations; explain both. Never export secrets in a convenience backup.

## Permissions and accessibility

Login/download/normal UI are non-admin. The OS, not a fake dialog, presents UAC. Show why elevation is needed before launching the helper; never ask for an admin password in the app. Preserve focus after cancel and provide keyboard navigation, accessible names, readable error text, high-DPI behavior and no color-only status.

Portable mode is a packaging choice, not a claim that no files or OS credentials are stored. Service mode may require protected locations outside Documents; this must be disclosed and approved, not hidden behind a checkbox.

## Preview limits

All six mock pages can be navigated without credentials, network, install privileges or external assets. Preview actions only change local HTML state and are labeled simulated. A result marked PASS is never shown as real machine evidence.
