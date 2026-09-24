# Platform references

Checked 2026-09-24. These are primary-source references for the design; recheck moving product details before implementing. No live runner, account registration or Windows behavior was tested by this reading.

## R1 — Runner scope and economics

https://docs.github.com/en/actions/concepts/runners/self-hosted-runners

Repository scope is one repository; organization scope can serve allowed organization repositories. Self-hosted compute is user-maintained. Do not confuse public product distribution with execution scope, or local compute with free artifact storage.

## R2 — Runner compatibility, connectivity, queue, updates

https://docs.github.com/en/actions/reference/runners/self-hosted-runners

Windows x64 is supported. Connections are outbound HTTPS. Offline jobs can wait and eventually expire; no silent hosted fallback is implied. Docker container actions/service containers require Linux. Runner update requirements need a maintenance design.

## R3 — Registration/removal authorization

https://docs.github.com/en/rest/actions/self-hosted-runners#create-a-registration-token-for-a-repository

Registration tokens expire after one hour. Repository administration access is required; fine-grained credentials need Administration write for this endpoint. Login, repository access and runner-administration permission are separate checks. Registration credentials and subsequent runner state are distinct secrets.

## R4 — Self-hosted trust

https://docs.github.com/en/actions/reference/security/secure-use#hardening-for-self-hosted-runners

Persistent runners can be compromised by workflow code. Public fork execution is unsafe on a personal/workplace machine. Private visibility alone is not a sandbox or a sufficient trust policy.

## R5 — Comment dispatch

https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#issue_comment

`issue_comment` supports created/edited/deleted events; the workflow must exist on the default branch. Its default context SHA is not automatically the requested PR/source SHA. Bind the target explicitly.

## R6 — Workflow token boundaries

https://docs.github.com/en/actions/concepts/security/github_token

`GITHUB_TOKEN` is scoped to the workflow repository. It cannot silently read another private repository or post there. Events caused by this token have recursion restrictions; verify the actual trigger rather than assuming bot comments dispatch jobs.

## R7 — Browser login option

https://cli.github.com/manual/gh_auth_login
https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps

CLI browser/device login is a possible adapter. Its documented plaintext fallback must not be silently accepted. A custom device flow requires an actual application identity/configuration; a GUI button alone is not an auth implementation. Avoid embedding client secrets in desktop releases.

## R8 — Windows UAC

https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecutew

The `runas` shell verb asks for administrator consent/credentials. It is different from bypassing policy. Elevating setup must not elevate job execution. Cancellation is an ordinary outcome.

## R9 — Windows service integration

https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/configure-the-application

Windows service setup is part of runner configuration and requires careful installation/identity handling. Do not invent a Windows `svc.cmd` API from Linux `svc.sh` instructions. Pin and inspect the chosen official runner package before building a GUI adapter.

## R10 — Application control

https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/appcontrol-and-applocker-overview

Application-control policy and UAC are separate mechanisms. Authorized elevation is not proof that every executable/script will run. Detect restrictions and explain the required administrator/IT approval; do not bypass them.

## R11 — Owner's prior UAC example (inspiration only)

https://github.com/bohanyt/windows_no_sleep/blob/cebc01c84027e0890aa828400078c84d38bdcdf3/native/WindowsNoSleep/app.manifest
https://github.com/bohanyt/windows_no_sleep/blob/cebc01c84027e0890aa828400078c84d38bdcdf3/native/WindowsNoSleep/ScreenSaverProtection.cs

The earlier source inspection in the owner conversation identified `asInvoker` plus an on-demand `runas` helper. This is not a security certification of that code, authorization to copy its privilege/IPC implementation, or evidence that this new product works. Do not alter Windows No Sleep or any system sleep/lock settings for this project.
