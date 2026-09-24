import { createHash } from 'node:crypto';

export const REQUEST_MARKER = '<!-- grl-request v1 -->';
export const ACK_MARKER = '<!-- grl-ack v1 -->';
export const RESULT_MARKER = '<!-- grl-result v1 -->';
export const REFUSAL_MARKER = '<!-- grl-exec-refusal v1 -->';
export const SHA = /^[0-9a-f]{40}$/;
export const SHA256 = /^[0-9a-f]{64}$/;
export const UUID4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;
export const REPOSITORY = /^[a-z0-9](?:[a-z0-9-]{0,37}[a-z0-9])?\/[a-z0-9](?:[a-z0-9._-]{0,98}[a-z0-9])?$/;
export const PROFILE_ID = /^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$/;
const UTC = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/;
const SECRET = /\b(?:ghu_|ghr_|ghp_|gho_|ghs_|github_pat_)[A-Za-z0-9_]+/gi;

export class ProtocolError extends Error {
  constructor(code) {
    super(code);
    this.name = 'ProtocolError';
    this.code = code;
  }
}

export const reject = code => { throw new ProtocolError(code); };
export const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');
export const gitBlobSha = bytes => {
  const data = Buffer.isBuffer(bytes) ? bytes : Buffer.from(bytes);
  return createHash('sha1').update(`blob ${data.length}\0`).update(data).digest('hex');
};
export const redact = text => String(text).replace(SECRET, '[REDACTED]');
export const encoded = value => Buffer.from(JSON.stringify(value), 'utf8').toString('base64');
export const decoded = value => parseStrictJson(Buffer.from(value, 'base64').toString('utf8'));

export function exactKeys(value, required, optional = []) {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) reject('EXPECTED_OBJECT');
  const allowed = new Set([...required, ...optional]);
  for (const key of Object.keys(value)) if (!allowed.has(key)) reject('UNKNOWN_FIELD');
  for (const key of required) if (!Object.hasOwn(value, key)) reject('MISSING_FIELD');
}

// JSON.parse validates syntax; this second lexical pass rejects duplicate keys
// at every object depth, including fields that a schema does not recognize.
export function parseStrictJson(text) {
  let value;
  try { value = JSON.parse(text); } catch { reject('INVALID_JSON'); }
  let i = 0;
  const space = () => { while (/\s/.test(text[i] ?? '') && i < text.length) i++; };
  const stringEnd = () => {
    const start = i++;
    while (i < text.length) {
      if (text[i] === '\\') { i += 2; continue; }
      if (text[i++] === '"') return JSON.parse(text.slice(start, i));
    }
    reject('INVALID_JSON');
  };
  const walk = depth => {
    if (depth > 32) reject('JSON_DEPTH');
    space();
    if (text[i] === '{') {
      i++;
      const seen = new Set();
      space();
      while (text[i] !== '}') {
        const key = stringEnd();
        if (seen.has(key)) reject('DUPLICATE_KEY');
        seen.add(key);
        space();
        i++; // colon: syntax already checked by JSON.parse
        walk(depth + 1);
        space();
        if (text[i] === ',') { i++; space(); } else break;
      }
      i++;
    } else if (text[i] === '[') {
      i++;
      space();
      while (text[i] !== ']') {
        walk(depth + 1);
        space();
        if (text[i] === ',') { i++; space(); } else break;
      }
      i++;
    } else if (text[i] === '"') {
      stringEnd();
    } else {
      while (i < text.length && !/[\s,}\]]/.test(text[i])) i++;
    }
  };
  walk(0);
  space();
  if (i !== text.length) reject('INVALID_JSON');
  return value;
}

export function utcMillis(text) {
  if (typeof text !== 'string' || !UTC.test(text)) reject('INVALID_TIME');
  const time = Date.parse(text);
  if (!Number.isFinite(time) || new Date(time).toISOString().slice(0, 19) !== text.slice(0, 19))
    reject('INVALID_TIME');
  return time;
}

export function parseRequestEnvelope(bodyBytes, { createdAt, now, repository, profilePolicies }) {
  const bytes = Buffer.isBuffer(bodyBytes) ? bodyBytes : Buffer.from(bodyBytes, 'utf8');
  if (bytes.length > 4096) reject('BODY_TOO_LARGE');
  if (bytes.subarray(0, 3).equals(Buffer.from([0xef, 0xbb, 0xbf]))) reject('BOM');
  let body;
  try { body = new TextDecoder('utf-8', { fatal: true }).decode(bytes); }
  catch { reject('INVALID_UTF8'); }
  if (body.split('```').length !== 3) reject('INVALID_ENVELOPE');
  const match = /^<!-- grl-request v1 -->\r?\n(?<fence>```json\r?\n(?<json>[\s\S]*?)\r?\n```)[ \t\r\n]*$/.exec(body);
  if (!match) reject('INVALID_ENVELOPE');
  const request = parseStrictJson(match.groups.json);
  exactKeys(request, ['schema_version', 'request_id', 'target', 'profile', 'expires_at', 'timeout_minutes']);
  if (request.schema_version !== 'grl.request.v1') reject('INVALID_VERSION');
  if (typeof request.request_id !== 'string' || !UUID4.test(request.request_id)) reject('INVALID_REQUEST_ID');
  exactKeys(request.target, ['repository', 'sha']);
  if (typeof request.target.repository !== 'string' || !REPOSITORY.test(request.target.repository) ||
      request.target.repository.includes('..')) reject('INVALID_REPOSITORY');
  if (request.target.repository !== repository) reject('STAGE1_REPOSITORY_MISMATCH');
  if (typeof request.target.sha !== 'string' || !SHA.test(request.target.sha)) reject('INVALID_SHA');
  exactKeys(request.profile, ['id', 'definition_sha']);
  if (typeof request.profile.id !== 'string' || !PROFILE_ID.test(request.profile.id) ||
      !Object.hasOwn(profilePolicies, request.profile.id)) reject('UNKNOWN_PROFILE');
  const policy = profilePolicies[request.profile.id];
  if (typeof request.profile.definition_sha !== 'string' || !SHA.test(request.profile.definition_sha))
    reject('INVALID_PROFILE_SHA');
  if (request.profile.definition_sha !== policy.definitionSha) reject('PROFILE_DEFINITION_MISMATCH');
  const expires = utcMillis(request.expires_at);
  const created = utcMillis(createdAt);
  if (expires < created + 60_000 || expires > created + 86_400_000) reject('EXPIRY_WINDOW');
  if (now >= expires) reject('EXPIRED');
  if (!Number.isInteger(request.timeout_minutes) || request.timeout_minutes < 1 ||
      request.timeout_minutes > policy.maxTimeoutMinutes) reject('TIMEOUT_OVER_MAX');
  return { request, bodySha256: sha256(Buffer.from(match.groups.fence, 'utf8')) };
}

export function validateProfile(bytes) {
  if (bytes.length > 32_768) reject('PROFILE_TOO_LARGE');
  const profile = parseStrictJson(Buffer.from(bytes).toString('utf8'));
  exactKeys(profile, ['id', 'version', 'max_timeout_minutes', 'min_tests',
    'estimated_disk_gib', 'required_capabilities', 'steps']);
  if (typeof profile.id !== 'string' || !PROFILE_ID.test(profile.id) ||
      profile.version !== 1 || !Number.isInteger(profile.max_timeout_minutes) ||
      profile.max_timeout_minutes < 1 || profile.max_timeout_minutes > 1440 ||
      !Number.isInteger(profile.min_tests) || profile.min_tests < 1 ||
      !Number.isFinite(profile.estimated_disk_gib) || profile.estimated_disk_gib < 0 ||
      !Array.isArray(profile.required_capabilities) ||
      !profile.required_capabilities.every(x => typeof x === 'string' && /^[a-z0-9._-]+$/.test(x)) ||
      !Array.isArray(profile.steps) || profile.steps.length < 1 || profile.steps.length > 20)
    reject('INVALID_PROFILE');
  for (const step of profile.steps) {
    exactKeys(step, ['name', 'executable', 'argv', 'timeout_seconds'], ['test_result']);
    if (typeof step.name !== 'string' || !/^[a-z0-9._-]+$/.test(step.name) ||
        typeof step.executable !== 'string' || !/^[a-z0-9._-]+$/.test(step.executable) ||
        !Array.isArray(step.argv) || step.argv.length > 32 ||
        !step.argv.every(x => typeof x === 'string' && x.length <= 512 && !/[\r\n\0]/.test(x)) ||
        !Number.isInteger(step.timeout_seconds) || step.timeout_seconds < 1 ||
        step.timeout_seconds > profile.max_timeout_minutes * 60) reject('INVALID_STEP');
    if (step.test_result) {
      exactKeys(step.test_result, ['kind', 'path']);
      if (!['junit', 'trx'].includes(step.test_result.kind) ||
          typeof step.test_result.path !== 'string' ||
          !/^[a-zA-Z0-9._/-]+$/.test(step.test_result.path) ||
          step.test_result.path.split('/').some(x => x === '..' || x === ''))
        reject('INVALID_TEST_RESULT');
    }
  }
  return { profile, definitionSha: gitBlobSha(bytes) };
}

export function fenced(marker, value, maxBytes) {
  const body = marker + '\n' + '```json\n' + JSON.stringify(value) + '\n```';
  if (Buffer.byteLength(body, 'utf8') > maxBytes) reject('COMMENT_TOO_LARGE');
  return body;
}
