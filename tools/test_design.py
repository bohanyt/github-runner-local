"""Regression tests for the design checker, not the future application."""
from __future__ import annotations
import json
import shutil
import tempfile
import unittest
from pathlib import Path
from check_design import fixture_errors, validate

ROOT = Path(__file__).resolve().parents[1]

class FixtureChecks(unittest.TestCase):
    def setUp(self):
        self.request = json.loads((ROOT / 'design/examples/request.json').read_text())
        self.result = json.loads((ROOT / 'design/examples/result.json').read_text())

    def reject(self, part):
        self.assertTrue(any(part in item for item in fixture_errors(self.request, self.result)), part)

    def test_valid_examples(self):
        self.assertEqual(fixture_errors(self.request, self.result), [])

    def test_non_objects(self):
        self.assertTrue(fixture_errors([], {}))

    def test_extra_request_command(self):
        self.request['command'] = 'arbitrary-shell'
        self.reject('request fields')

    def test_must_stay_example_only(self):
        self.request['example_only'] = False
        self.reject('example_only')

    def test_full_sha_required(self):
        self.request['target']['sha'] = 'abc123'
        self.reject('full lowercase hex')

    def test_target_type(self):
        self.request['target'] = []
        self.reject('target must')

    def test_request_identity_matches(self):
        self.result['request_id'] = 'b70dfe32-244d-4b79-a781-523a0b828448'
        self.reject('request_id mismatch')

    def test_target_repo_matches(self):
        self.result['target']['repository'] = 'example/other'
        self.reject('target mismatch')

    def test_tested_sha_matches(self):
        self.result['tested_sha'] = 'c' * 40
        self.reject('tested_sha mismatch')

    def test_profile_matches(self):
        self.result['profile']['definition_sha'] = 'c' * 40
        self.reject('profile mismatch')

    def test_nonzero_exit_not_pass(self):
        self.result['checks'][0]['exit_code'] = 1
        self.reject('PASS contradicts')

    def test_failed_tests_not_pass(self):
        self.result['checks'][0]['failed'] = 1
        self.reject('PASS contradicts')

    def test_negative_counts(self):
        self.result['checks'][0]['passed'] = -1
        self.reject('nonnegative')

    def test_boolean_not_integer_count(self):
        self.result['checks'][0]['passed'] = True
        self.reject('nonnegative')

    def test_no_admin_fixture(self):
        self.result['runner']['elevated'] = True
        self.reject('non-elevated')

    def test_timeout_bound(self):
        self.request['timeout_minutes'] = 0
        self.reject('timeout')

    def test_expiry_timezone(self):
        self.request['expires_at'] = '2026-09-24T12:00:00'
        self.reject('timezone')

    def test_report_incomplete_is_distinct(self):
        self.result['reporting_status'] = 'REPORTING_INCOMPLETE'
        self.assertEqual(fixture_errors(self.request, self.result), [])

    def test_run_attempt_positive(self):
        self.result['run']['attempt'] = 0
        self.reject('run identity')

    def test_real_evidence_claim_rejected(self):
        self.result['notes'] = ['All tests actually passed.']
        self.reject('invented evidence')

class PackChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        shutil.copytree(ROOT / 'design', self.root / 'design')

    def test_valid_pack(self):
        self.assertEqual(validate(self.root), [])

    def test_missing_doc(self):
        (self.root / 'design/THREAT_MODEL.md').unlink()
        self.assertTrue(any('missing design/THREAT_MODEL.md' in e for e in validate(self.root)))

    def test_broken_link(self):
        with (self.root / 'design/README.md').open('a') as f:
            f.write('\n[missing](missing.md)\n')
        self.assertTrue(any('broken local link' in e for e in validate(self.root)))

    def test_no_live_workflow(self):
        path = self.root / '.github/workflows/live.yaml'
        path.parent.mkdir(parents=True)
        path.write_text('on: push\n')
        self.assertTrue(any('live workflow' in e for e in validate(self.root)))

    def test_no_network_preview(self):
        with (self.root / 'design/wizard-preview.html').open('a') as f:
            f.write('<script>fetch("https://example.com")</script>')
        self.assertTrue(any('network-capable' in e for e in validate(self.root)))

    def test_corrupt_json(self):
        (self.root / 'design/examples/result.json').write_text('{broken')
        self.assertTrue(any('fixture JSON' in e for e in validate(self.root)))

if __name__ == '__main__':
    unittest.main()
