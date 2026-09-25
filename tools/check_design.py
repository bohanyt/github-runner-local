"""Offline design-pack checks only; not a live request parser or security boundary."""
from __future__ import annotations
import json
import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlsplit
from uuid import UUID
from datetime import datetime

REQUIRED = (
    'design/README.md', 'design/ARCHITECTURE.md', 'design/WIZARD.md',
    'design/THREAT_MODEL.md', 'design/REQUEST_RESULT_PROTOCOL.md',
    'design/ACCEPTANCE.md', 'design/wizard-preview.html',
    'design/examples/request.json', 'design/examples/result.json',
)
SHA = re.compile(r'^[0-9a-f]{40}$')
REPO = re.compile(r'^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')
REQUEST_KEYS = {
    'schema_version', 'example_only', 'request_id', 'target', 'profile',
    'machine_selector', 'expires_at', 'timeout_minutes',
}


def fixture_errors(request: object, result: object) -> list[str]:
    """Validate the invented v0 examples. Never use this to authorize execution."""
    errors: list[str] = []
    if not isinstance(request, dict) or not isinstance(result, dict):
        return ['fixtures must be JSON objects']
    if set(request) != REQUEST_KEYS:
        errors.append('request fields differ from example contract')
    for name, obj, version in (
        ('request', request, 'grl.request.v0'), ('result', result, 'grl.result.v0')
    ):
        if obj.get('schema_version') != version:
            errors.append(f'{name}: wrong schema version')
        if obj.get('example_only') is not True:
            errors.append(f'{name}: example_only must be true')
        try:
            UUID(str(obj.get('request_id', '')))
        except ValueError:
            errors.append(f'{name}: invalid request_id')
        target = obj.get('target')
        if not isinstance(target, dict):
            errors.append(f'{name}: target must be an object')
        else:
            if set(target) != {'repository', 'sha'}:
                errors.append(f'{name}: invalid target fields')
            if not REPO.fullmatch(str(target.get('repository', ''))):
                errors.append(f'{name}: invalid repository')
            if not SHA.fullmatch(str(target.get('sha', ''))):
                errors.append(f'{name}: target SHA must be full lowercase hex')
        profile = obj.get('profile')
        if not isinstance(profile, dict) or set(profile) != {'id', 'definition_sha'}:
            errors.append(f'{name}: invalid profile fields')
        elif profile['id'] != 'smoke-fixture' or not SHA.fullmatch(str(profile['definition_sha'])):
            errors.append(f'{name}: invalid invented fixture profile')
    for key in ('request_id', 'target', 'profile'):
        if request.get(key) != result.get(key):
            errors.append(f'result {key} mismatch')
    target = request.get('target')
    expected_sha = target.get('sha') if isinstance(target, dict) else None
    if result.get('tested_sha') != expected_sha or not SHA.fullmatch(str(result.get('tested_sha', ''))):
        errors.append('tested_sha mismatch or malformed')
    if not SHA.fullmatch(str(result.get('workflow_sha', ''))):
        errors.append('workflow_sha malformed')
    timeout = request.get('timeout_minutes')
    if type(timeout) is not int or not 1 <= timeout <= 60:
        errors.append('timeout must be an integer within the illustrative bound')
    try:
        expiry = datetime.fromisoformat(str(request.get('expires_at', '')).replace('Z', '+00:00'))
        if expiry.tzinfo is None:
            raise ValueError('timezone missing')
    except ValueError:
        errors.append('expires_at must contain a valid timezone-aware time')
    run = result.get('run')
    if not isinstance(run, dict) or any(type(run.get(k)) is not int or run[k] <= 0 for k in ('id', 'job_id', 'attempt')):
        errors.append('result run identity malformed')
    runner = result.get('runner')
    if not isinstance(runner, dict) or runner.get('elevated') is not False:
        errors.append('example runner must be non-elevated')
    if result.get('execution_status') not in {'PASS', 'FAIL', 'BLOCKED', 'CANCELLED', 'TIMED_OUT'}:
        errors.append('unknown execution_status')
    if result.get('reporting_status') not in {'COMPLETE', 'REPORTING_INCOMPLETE'}:
        errors.append('unknown reporting_status')
    checks = result.get('checks')
    if not isinstance(checks, list) or not checks:
        errors.append('checks must be a nonempty list in this fixture')
    else:
        for check in checks:
            if not isinstance(check, dict):
                errors.append('check must be an object')
                continue
            if any(type(check.get(k)) is not int or check[k] < 0 for k in ('passed', 'failed', 'skipped')):
                errors.append('check counts must be nonnegative integers')
            if type(check.get('exit_code')) is not int:
                errors.append('check exit_code must be an integer')
            if result.get('execution_status') == 'PASS' and (check.get('exit_code') != 0 or check.get('failed') != 0):
                errors.append('PASS contradicts check outcome')
    if not isinstance(result.get('artifacts'), list):
        errors.append('artifacts must be an explicit list')
    notes = result.get('notes')
    if not isinstance(notes, list) or not any('INVENTED EXAMPLE' in str(note) for note in notes):
        errors.append('result must explicitly disclaim invented evidence')
    return errors


def validate(root: Path) -> list[str]:
    """Check local files only. No network, execution, credentials or GitHub calls."""
    root = root.resolve()
    errors = [f'missing {name}' for name in REQUIRED if not (root / name).is_file()]
    workflows = root / '.github' / 'workflows'
    if workflows.exists() and any(workflows.rglob('*.yml')):
        errors.append('live workflow YAML is forbidden during bootstrap')
    if workflows.exists() and any(workflows.rglob('*.yaml')):
        errors.append('live workflow YAML is forbidden during bootstrap')
    for md in sorted((root / 'design').rglob('*.md')):
        text = md.read_text(encoding='utf-8')
        for link in re.findall(r'\[[^\]]*\]\(([^)]+)\)', text):
            url = urlsplit(link)
            if url.scheme or link.startswith('#') or not url.path:
                continue
            destination = (md.parent / unquote(url.path)).resolve()
            if not destination.is_relative_to(root) or not destination.exists():
                errors.append(f'broken local link in {md.name}: {link}')
    try:
        request = json.loads((root / 'design/examples/request.json').read_text(encoding='utf-8'))
        result = json.loads((root / 'design/examples/result.json').read_text(encoding='utf-8'))
        errors.extend(fixture_errors(request, result))
    except (OSError, ValueError) as exc:
        errors.append(f'cannot read fixture JSON: {exc}')
    preview = root / 'design/wizard-preview.html'
    if preview.is_file():
        html = preview.read_text(encoding='utf-8')
        if 'OFFLINE DESIGN PREVIEW' not in html or "connect-src 'none'" not in html:
            errors.append('preview must disclose offline mode and deny network connections')
        if re.search(r'fetch\s*\(|XMLHttpRequest|WebSocket|sendBeacon|window\.open\s*\(|(?:src|href)\s*=\s*[\"\x27]https?://', html, re.I):
            errors.append('preview includes a prohibited network-capable construct')
        if len(re.findall(r'data-panel="\d"', html)) != 7:
            errors.append('preview must contain seven mock panels')
    return errors


def main() -> int:
    root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    errors = validate(root)
    if errors:
        print('\n'.join('FAIL: ' + error for error in errors))
        return 1
    print('PASS: offline design pack, links, invented fixtures and preview guardrails')
    print('NOT TESTED: Windows, UAC, auth, runner registration, job execution, isolation or GitHub delivery')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
